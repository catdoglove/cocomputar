using UnityEngine;
namespace CoComputar
{
    [CreateAssetMenu(menuName = "Co-computar/Stage")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string stageId, nextStageId, title, clearCondition = "door.exe";
        public GameObject entryPrefab;
        public TextAsset dialogue, files, folderTree, letters;
        public bool hasPrologue;
        public bool preserveWindowsDuringDialogue;
        public DesktopTheme desktopTheme;
    }
}
