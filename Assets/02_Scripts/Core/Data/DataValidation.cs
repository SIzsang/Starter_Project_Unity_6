using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarterProject
{
    /// <summary>등록 목록의 ID·필수 참조·게임별 규칙을 저장 읽기와 씬 진입 전에 검증합니다.</summary>
    public static class DataValidation
    {
        public static IReadOnlyDictionary<string, DataDefinition> Validate(DataCatalog catalog)
        {
            var definitions = new Dictionary<string, DataDefinition>(StringComparer.Ordinal);
            if (catalog == null) return new ReadOnlyDictionary<string, DataDefinition>(definitions);
            foreach (var definition in catalog.Definitions)
            {
                if (definition == null) throw new InvalidOperationException("Data Catalog contains a missing Definition.");
                if (string.IsNullOrWhiteSpace(definition.Id))
                    throw new InvalidOperationException("Data Definition requires an ID.");
                foreach (var character in definition.Id)
                    if (char.IsWhiteSpace(character) || char.IsControl(character))
                        throw new InvalidOperationException($"Data Definition ID contains whitespace or control characters: {definition.Id}");
                if (definitions.ContainsKey(definition.Id))
                    throw new InvalidOperationException($"Duplicate Data Definition ID: {definition.Id}");
                definitions.Add(definition.Id, definition);
            }
            var snapshot = new ReadOnlyDictionary<string, DataDefinition>(definitions);
            foreach (var definition in snapshot.Values)
            {
                foreach (var reference in definition.References)
                    if (string.IsNullOrEmpty(reference) || !snapshot.ContainsKey(reference))
                        throw new InvalidOperationException($"Data Definition {definition.Id} has a missing reference: {reference}");
                definition.Validate(snapshot);
            }
            return snapshot;
        }
    }
}