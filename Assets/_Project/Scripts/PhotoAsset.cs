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
    // A resolução das fotografias encontradas. Nada a ver com os 256×256 da
    // RenderTexture de captura: aquilo é o que o jogador tira, isto é o que
    // ele acha e vai ter de examinar ao pormenor.
    public const int LarguraAutorada = 2048;
    public const int AlturaAutorada = 1368;

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

        if (fotografia.ImagemBase == null)
        {
            Debug.LogWarning(
                $"{name}: fotografia sem imagem — aparece em branco no álbum.",
                this
            );
        }
        else if (fotografia.ImagemBase.width != LarguraAutorada ||
                 fotografia.ImagemBase.height != AlturaAutorada)
        {
            // As fotografias encontradas são desenhadas em ecrã inteiro no
            // viewer e sobrepostas na comparação: abaixo desta resolução
            // perde-se o detalhe que o jogador tem de encontrar na imagem.
            // Os 2048×1368 são múltiplos de 4 (exigência de compressão) e
            // mantêm a proporção 3:2 da fotografia de 35 mm.
            Debug.LogWarning(
                $"{name}: a imagem é {fotografia.ImagemBase.width}×" +
                $"{fotografia.ImagemBase.height} e as fotografias autoradas são " +
                $"{LarguraAutorada}×{AlturaAutorada}.",
                this
            );
        }

        foreach (PhotoVariante variante in fotografia.Variantes)
        {
            if (variante == null)
                continue;

            // Uma variante sem stateId nunca vale, e ninguém dá por ela até a
            // fotografia não mudar no regresso do flashback.
            if (string.IsNullOrWhiteSpace(variante.StateId))
            {
                Debug.LogWarning(
                    $"{name}: variante sem stateId — nunca chega a valer.",
                    this
                );
            }

            // A comparação sobrepõe a versão alterada à mesma fotografia de
            // outra década: com outra proporção, o alinhamento que era certo
            // deixava de o ser só porque o passado mudou.
            if (variante.Imagem != null && fotografia.ImagemBase != null &&
                (variante.Imagem.width != fotografia.ImagemBase.width ||
                 variante.Imagem.height != fotografia.ImagemBase.height))
            {
                Debug.LogWarning(
                    $"{name}: a variante {variante.Id} tem imagem de " +
                    $"{variante.Imagem.width}×{variante.Imagem.height} e a " +
                    $"base de {fotografia.ImagemBase.width}×" +
                    $"{fotografia.ImagemBase.height}.",
                    this
                );
            }
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
