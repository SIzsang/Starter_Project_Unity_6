using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarterProject
{
    /// <summary>Boot에서 사용하는 선택형 Definition 목록입니다. 비어 있어도 Core는 동작합니다.</summary>
    [CreateAssetMenu(menuName = "Starter Project/Data Catalog")]
    public sealed class DataCatalog : ScriptableObject
    {
        [SerializeField] private DataDefinition[] definitions = Array.Empty<DataDefinition>();
        public IReadOnlyList<DataDefinition> Definitions => Array.AsReadOnly(definitions ?? Array.Empty<DataDefinition>());
    }
}