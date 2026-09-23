using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarterProject
{
    public enum StorageStatus { Missing, Loaded, Recovered, Invalid, UnsupportedVersion, IoError }

    public sealed class LoadResult<T> where T : class
    {
        public StorageStatus Status { get; }
        public T Value { get; }
        public string Message { get; }
        public bool HasValue => Value != null;
        public LoadResult(StorageStatus status, T value = null, string message = "")
        { Status = status; Value = value; Message = message; }
    }

    public sealed class UnsupportedDataVersionException : Exception
    {
        public UnsupportedDataVersionException(string message) : base(message) { }
    }

    /// <summary>작은 파일 경계입니다. 쓰기 실패 시 기존 주 파일을 보존하는 구현만 대체할 수 있습니다.</summary>
    public interface ITextFileStore
    {
        string ReadAllText(string fileName);
        void WriteAllText(string fileName, string contents, Func<string, bool> validateContents, bool preserveOriginal);
    }

    /// <summary>작은 로컬 JSON용 동기 저장소. 같은 디렉터리의 임시 파일을 검증 후 교체합니다.</summary>
    public sealed class JsonFileStore : ITextFileStore
    {
        public const int MaxFileSizeBytes = 1024 * 1024;
        private readonly string directoryPath;
        private readonly object syncRoot = new object();

        public JsonFileStore(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException("Storage path is empty.");
            this.directoryPath = Path.GetFullPath(directoryPath);
        }

        private string GetFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName)
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName == "." || fileName == "..")
                throw new ArgumentException("Use a file name without directory components.");
            return Path.Combine(directoryPath, fileName);
        }

        public string ReadAllText(string fileName)
        {
            lock (syncRoot)
            {
                var filePath = GetFilePath(fileName);
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (fileStream.Length > MaxFileSizeBytes) throw new InvalidDataException("Save file exceeds 1 MiB.");
                        using (var reader = new StreamReader(fileStream, new UTF8Encoding(false, true)))
                            return reader.ReadToEnd();
                    }
                }
                catch (FileNotFoundException) { return null; }
                catch (DirectoryNotFoundException)
                {
                    // 파일이 디렉터리를 막고 있는 경우는 첫 실행으로 오인하지 않습니다.
                    if (File.Exists(directoryPath)) throw new IOException("Storage directory is a file.");
                    return null;
                }
            }
        }

        public void WriteAllText(string fileName, string contents, Func<string, bool> validateContents, bool preserveOriginal)
        {
            lock (syncRoot)
            {
                var filePath = GetFilePath(fileName);
                var bytes = new UTF8Encoding(false, true).GetBytes(contents);
                if (bytes.Length > MaxFileSizeBytes) throw new InvalidDataException("Save file exceeds 1 MiB.");
                Directory.CreateDirectory(directoryPath);
                var tempFilePath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        fileStream.Write(bytes, 0, bytes.Length);
                        fileStream.Flush(true);
                    }
                    if (!validateContents(File.ReadAllText(tempFilePath, Encoding.UTF8)))
                        throw new InvalidDataException("Temporary save verification failed.");
                    if (File.Exists(filePath))
                    {
                        var backupFilePath = preserveOriginal ? filePath + ".preserved-" + Guid.NewGuid().ToString("N") : filePath + ".bak";
                        // 교체 실패 시 삭제 후 이동으로 우회하지 않습니다. 기존 파일을 보호합니다.
                        File.Replace(tempFilePath, filePath, backupFilePath);
                    }
                    else File.Move(tempFilePath, filePath);
                }
                finally
                {
                    try { if (File.Exists(tempFilePath)) File.Delete(tempFilePath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
    }

    public static class JsonData
    {
        public static JObject ParseObject(string jsonText)
        {
            using (var reader = new JsonTextReader(new StringReader(jsonText)))
            {
                reader.DateParseHandling = DateParseHandling.None;
                reader.MaxDepth = 32;
                var jsonObject = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new JsonException("Unexpected content after JSON object.");
                return jsonObject;
            }
        }

        public static int GetSchemaVersion(JObject jsonObject, int currentVersion)
        {
            var versionToken = jsonObject["schemaVersion"];
            if (versionToken == null || versionToken.Type != JTokenType.Integer) throw new JsonException("schemaVersion must be an integer.");
            if (!long.TryParse(versionToken.ToString(Formatting.None), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var schemaVersion) || schemaVersion < 1 || schemaVersion > currentVersion)
                throw new UnsupportedDataVersionException("This file version is not supported. The original file is protected.");
            return (int)schemaVersion;
        }
    }

    /// <summary>주 파일 우선. 파손 시 백업을 메모리로 읽되 명시적 복구 전에는 원본을 바꾸지 않습니다.</summary>
    public sealed class JsonRepository<T> where T : class
    {
        private readonly ITextFileStore fileStore;
        private readonly string fileName;
        private readonly Func<string, T> deserialize;
        private readonly Func<T, string> serialize;
        private readonly object syncRoot = new object();

        public JsonRepository(ITextFileStore fileStore, string fileName, Func<string, T> deserialize, Func<T, string> serialize)
        { this.fileStore = fileStore; this.fileName = fileName; this.deserialize = deserialize; this.serialize = serialize; }

        private LoadResult<T> LoadFile(string fileName)
        {
            try
            {
                var jsonText = fileStore.ReadAllText(fileName);
                return jsonText == null ? new LoadResult<T>(StorageStatus.Missing)
                    : new LoadResult<T>(StorageStatus.Loaded, deserialize(jsonText));
            }
            catch (UnsupportedDataVersionException exception) { return new LoadResult<T>(StorageStatus.UnsupportedVersion, message: exception.Message); }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException || exception is FormatException
                || exception is OverflowException || exception is DecoderFallbackException)
            { return new LoadResult<T>(StorageStatus.Invalid, message: "Invalid save data. " + exception.Message); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { return new LoadResult<T>(StorageStatus.IoError, message: "Could not access save data. " + exception.Message); }
        }

        public LoadResult<T> Load()
        {
            lock (syncRoot)
            {
                var primaryResult = LoadFile(fileName);
                if (primaryResult.Status != StorageStatus.Invalid && primaryResult.Status != StorageStatus.Missing) return primaryResult;
                var backupResult = LoadFile(fileName + ".bak");
                if (backupResult.HasValue)
                    return new LoadResult<T>(StorageStatus.Recovered, backupResult.Value, "Backup available. Select Recover Backup before saving.");
                if (primaryResult.Status == StorageStatus.Missing && backupResult.Status != StorageStatus.Missing) return backupResult;
                return primaryResult;
            }
        }

        public bool TrySave(T value, out string errorMessage, bool allowOverwriteInvalidFile = false)
        {
            lock (syncRoot)
            {
                var loadResult = Load();
                if (loadResult.Status == StorageStatus.UnsupportedVersion || loadResult.Status == StorageStatus.IoError
                    || loadResult.Status == StorageStatus.Recovered || loadResult.Status == StorageStatus.Invalid && !allowOverwriteInvalidFile)
                { errorMessage = loadResult.Message; return false; }
                return TryWriteFile(value, loadResult.Status == StorageStatus.Invalid, out errorMessage);
            }
        }

        public bool TryRecoverBackup(out string errorMessage)
        {
            lock (syncRoot)
            {
                var loadResult = Load();
                if (loadResult.Status != StorageStatus.Recovered)
                { errorMessage = "No recoverable backup is available."; return false; }
                return TryWriteFile(loadResult.Value, true, out errorMessage);
            }
        }

        private bool TryWriteFile(T value, bool preserveOriginal, out string errorMessage)
        {
            try
            {
                var jsonText = serialize(value);
                deserialize(jsonText);
                fileStore.WriteAllText(fileName, jsonText, contents => { deserialize(contents); return true; }, preserveOriginal);
                errorMessage = "";
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException
                || exception is InvalidDataException || exception is UnsupportedDataVersionException || exception is ArgumentException || exception is NotSupportedException)
            { errorMessage = "Save failed. " + exception.Message; return false; }
        }
    }
}
