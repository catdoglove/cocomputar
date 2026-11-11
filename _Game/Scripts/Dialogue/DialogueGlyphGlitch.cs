using TMPro;
using UnityEngine;
namespace CoComputar
{
    public sealed class DialogueGlyphGlitch:MonoBehaviour
    {
        public string[] Words;
        void LateUpdate()
        {
            if(Words==null||Words.Length==0)return;var text=GetComponent<TextMeshProUGUI>();text.ForceMeshUpdate();
            for(int i=0;i<text.textInfo.characterCount;i++)
            {
                var c=text.textInfo.characterInfo[i];if(!c.isVisible||i>=text.maxVisibleCharacters||!System.Array.Exists(Words,w=>w.Contains(c.character.ToString())))continue;
                var mesh=text.textInfo.meshInfo[c.materialReferenceIndex];float dx=Mathf.Sin(Time.unscaledTime*43+i)*1.2f;
                for(int j=0;j<4;j++){mesh.vertices[c.vertexIndex+j]+=new Vector3(dx,0,0);mesh.colors32[c.vertexIndex+j].a=(byte)(180+75*Mathf.Abs(Mathf.Sin(Time.unscaledTime*15)));}
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices|TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
