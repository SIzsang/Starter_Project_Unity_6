using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarterProject
{
    /// <summary>읽기 전용으로 사용하는 Definition 원본입니다. 게임별 고정 필드는 파생 SO에 둡니다.</summary>
    [CreateAssetMenu(menuName = "Starter Project/Data Definition")]
    public class DataDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string[] references = Array.Empty<string>();
        public string Id => id;
        public IReadOnlyList<string> References => Array.AsReadOnly(references ?? Array.Empty<string>());
        /// <summary>게임별 추가 규칙만 검증합니다. 원본 변경·I/O 없이 반복 호출 가능해야 합니다.</summary>
        public virtual void Validate(IReadOnlyDictionary<string, DataDefinition> definitions) { }
    }
}