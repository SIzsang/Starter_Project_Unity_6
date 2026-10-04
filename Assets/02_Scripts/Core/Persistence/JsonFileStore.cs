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

    /// <summary>작은 동기 텍스트 저장 경계입니다. AppRoot와 Repository는 구현을 Dispose하지 않습니다.</summary>
    /// <remarks>해제할 자원이 있는 대체 구현은 주입자가 수명을 관리합니다. 다중 프로세스의 동시 쓰기는 계약에 포함하지 않습니다.</remarks>
    public interface ITextFileStore
    {
        /// <summary>주어진 파일명 또는 백업 파일명을 읽습니다. 파일이 없을 때만 null을 반환합니다.</summary>
        /// <exception cref="IOException">잠금·저장 위치 등 접근 실패입니다. 파일 부재로 숨기지 않습니다.</exception>
        /// <exception cref="UnauthorizedAccessException">접근 권한이 없습니다.</exception>
        /// <exception cref="InvalidDataException">파일이 구현의 데이터 크기 제한 등을 위반합니다.</exception>
        string ReadAllText(string fileName);

        /// <summary>기록할 내용을 검증한 뒤 교체합니다. 검증이 false를 반환하거나 예외가 나면 기존 주 파일을 보존합니다.</summary>
        /// <param name="fileName">디렉터리 부분이 없는 파일명입니다.</param>
        /// <param name="contents">새로 기록할 전체 텍스트입니다.</param>
        /// <param name="validateContents">교체 전 저장 내용을 확인합니다. 반복 호출해도 안전하고 부작용이 없어야 합니다.</param>
        /// <param name="preserveOriginal">false이면 기존 주 파일을 .bak으로 보관합니다. true이면 기존 .bak을 유지하고 원본을 별도 preserved 사본으로 보관합니다.</param>
        /// <exception cref="IOException">쓰기·교체 실패입니다. 기존 주 파일을 먼저 삭제하는 우회를 하지 않습니다.</exception>
        /// <exception cref="UnauthorizedAccessException">쓰기 권한이 없습니다.</exception>
        /// <exception cref="InvalidDataException">크기 제한 또는 저장 내용 검증에 실패했습니다. 검증기가 던진 예외도 호출자에게 전달합니다.</exception>
        void WriteAllText(string fileName, string contents, Func<string, bool> validateContents, bool preserveOriginal);
    }

    /// <summary>한 저장 파일과 해당 구현이 생성한 복구·임시 파일을 함께 삭제하는 선택적 기능입니다.</summary>
    public interface IDeleteSaveFileStore
    {
        /// <summary>보조 파일을 먼저 삭제하고 주 파일을 마지막에 삭제합니다. 모든 대상이 이미 없으면 성공합니다.</summary>
        /// <remarks>실패 시 예외를 전달하며 일부 보조 파일은 이미 삭제되었을 수 있습니다. 다른 파일·디렉터리는 삭제하지 않습니다.</remarks>
        /// <param name="fileName">삭제할 주 파일명입니다. 디렉터리 부분은 허용하지 않습니다.</param>
        /// <exception cref="ArgumentException">허용하지 않는 파일명입니다.</exception>
        /// <exception cref="IOException">파일 접근·삭제에 실패했습니다.</exception>
        /// <exception cref="UnauthorizedAccessException">파일 삭제 권한이 없습니다.</exception>
        void DeleteSaveFiles(string fileName);
    }

    /// <summary>작은 로컬 JSON용 동기 저장소. 같은 디렉터리의 임시 파일을 검증 후 교체합니다.</summary>
    public sealed class JsonFileStore : ITextFileStore, IDeleteSaveFileStore
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

        /// <summary>이 저장소가 만든 정확한 보조 파일만 지워 백업에 의한 저장 재등장을 막습니다.</summary>
        public void DeleteSaveFiles(string fileName)
        {
            lock (syncRoot)
            {
                var filePath = GetFilePath(fileName);
                string[] candidates;
                try { candidates = Directory.GetFiles(directoryPath); }
                catch (DirectoryNotFoundException)
                {
                    if (File.Exists(directoryPath)) throw new IOException("Storage directory is a file.");
                    return;
                }

                // 복구 후보를 먼저 지웁니다. 접근 실패를 숨기거나 주 파일 삭제를 계속하지 않습니다.
                File.Delete(filePath + ".bak");
                foreach (var candidate in candidates)
                {
                    var candidateName = Path.GetFileName(candidate);
                    if (HasGeneratedFileName(candidateName, fileName + ".preserved-", "")
                        || HasGeneratedFileName(candidateName, fileName + ".", ".tmp"))
                        File.Delete(candidate);
                }
                File.Delete(filePath);
            }
        }

        private static bool HasGeneratedFileName(string candidate, string prefix, string suffix)
        {
            return candidate.Length == prefix.Length + 32 + suffix.Length
                && candidate.StartsWith(prefix, StringComparison.Ordinal)
                && candidate.EndsWith(suffix, StringComparison.Ordinal)
                && Guid.TryParseExact(candidate.Substring(prefix.Length, 32), "N", out _);
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
                || exception is InvalidDataException || exception is UnsupportedDataVersionException || exception is ArgumentException || exception is NotSupportedException
                || exception is FormatException || exception is OverflowException)
            { errorMessage = "Save failed. " + exception.Message; return false; }
        }
    }
}
