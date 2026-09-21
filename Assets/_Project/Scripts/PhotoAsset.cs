using UnityEngine;

// A fotografia autorada no editor — as que o jogador ENCONTRA. É o primeiro
// ScriptableObject do projecto: até aqui os dados viviam todos em
// [SerializeField] dentro de MonoBehaviours, o que serve para um objecto que
// está numa cena e não para conteúdo que a FASE 18 vai produzir às centenas e
// que a FASE 21 tem de gravar.
//
// Guarda um PhotoData e entrega sempre uma CÓPIA dele a quem o pede. O asset é
// a autoria e não o estado: o estado — que descobertas já foram vistas — é do
// jogo, e escrevê-lo aqui sujava o ficheiro em disco.
[CreateAssetMenu(
    fileName = "Fotografia",
    menuName = "Pine Hollow/Fotografia",
    order = 0
)]
public class PhotoAsset : ScriptableObject
{
    [SerializeField] private PhotoData fotografia = new PhotoData();

    // Cópia nova a cada chamada. Quem carregar a mesma fotografia duas vezes
    // fica com duas, e é o que se quer: o álbum não deve partilhar estado com
    // o asset.
    public PhotoData Criar()
    {
        return fotografia.CopiaAutorada(name);
    }

    // Corre no editor a cada alteração no inspector. Não impede nada — só se
    // queixa, que é o que este projecto faz com dependências mal preenchidas.
    private void OnValidate()
    {
        if (fotografia == null)
            return;

        if (fotografia.Imagem == null)
        {
            Debug.LogWarning(
                $"{name}: fotografia sem imagem — aparece em branco no álbum.",
                this
            );
        }

        if (fotografia.Ano != 0 && !AnoCanonico(fotografia.Ano))
        {
            // Os anos do GDD não são decoração: é a coincidência do Thomas
            // Hale entre eles que sustenta a história. Um ano inventado passa
            // despercebido até alguém tentar montar a cronologia.
            Debug.LogWarning(
                $"{name}: o ano {fotografia.Ano} não é dos anos do canon " +
                "(1946, 1958, 1971, 1986, 1994, 2001, 2011, 2016, 2026).",
                this
            );
        }
    }

    private static bool AnoCanonico(int ano)
    {
        foreach (int canonico in PhotoData.AnosCanonicos)
        {
            if (canonico == ano)
                return true;
        }

        return false;
    }
}
