using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Testes da lógica da pilha de modos. O efeito real em Cursor.lockState não se
// testa aqui — testa-se a propriedade IsCursorFree, que é o que o decide.
public class PlayerStateMachineTests
{
    private GameObject host;
    private PlayerStateMachine stateMachine;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("PlayerStateMachineTests");
        stateMachine = host.AddComponent<PlayerStateMachine>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
    }

    [Test]
    public void PilhaVazia_EstadoENormal_ENadaBloqueia()
    {
        Assert.AreEqual(PlayerState.Normal, stateMachine.Current);
        Assert.AreEqual(0, stateMachine.OpenModeCount);
        Assert.IsFalse(stateMachine.IsMovementLocked);
        Assert.IsFalse(stateMachine.IsLookLocked);
        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    [Test]
    public void Locomocao_ValeEnquantoNaoHaCamadas()
    {
        stateMachine.SetLocomotion(PlayerState.Sprinting);

        Assert.AreEqual(PlayerState.Sprinting, stateMachine.Current);
        Assert.IsFalse(stateMachine.IsMovementLocked);

        stateMachine.SetLocomotion(PlayerState.Crouching);

        Assert.AreEqual(PlayerState.Crouching, stateMachine.Current);
        Assert.IsFalse(stateMachine.IsMovementLocked);
    }

    [Test]
    public void SetLocomocao_RecusaOQueNaoELocomocao()
    {
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
            "SetLocomotion só aceita"));

        stateMachine.SetLocomotion(PlayerState.Inspecting);

        Assert.AreEqual(PlayerState.Normal, stateMachine.Current);
    }

    [Test]
    public void CamadaAberta_MandaSobreALocomocao()
    {
        stateMachine.SetLocomotion(PlayerState.Sprinting);
        stateMachine.PushMode(PlayerState.Inspecting);

        Assert.AreEqual(PlayerState.Inspecting, stateMachine.Current);

        // A locomoção não se perde: volta quando a camada fecha.
        stateMachine.PopMode(PlayerState.Inspecting);

        Assert.AreEqual(PlayerState.Sprinting, stateMachine.Current);
    }

    [Test]
    public void Inspeccao_BloqueiaMovimentoELook()
    {
        stateMachine.PushMode(PlayerState.Inspecting);

        Assert.IsTrue(stateMachine.IsMovementLocked);
        Assert.IsTrue(stateMachine.IsLookLocked);
        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    [Test]
    public void ModoFotografia_BloqueiaMovimentoMasDeixaApontar()
    {
        stateMachine.PushMode(PlayerState.Photographing);

        Assert.IsTrue(stateMachine.IsMovementLocked);
        Assert.IsFalse(stateMachine.IsLookLocked);
        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    [Test]
    public void ModosDeRato_LibertamOCursor()
    {
        stateMachine.PushMode(PlayerState.ViewingAlbum);
        Assert.IsTrue(stateMachine.IsCursorFree);

        stateMachine.PushMode(PlayerState.ViewingPhoto);
        Assert.IsTrue(stateMachine.IsCursorFree);

        stateMachine.PopMode(PlayerState.ViewingPhoto);
        Assert.IsTrue(stateMachine.IsCursorFree);

        stateMachine.PopMode(PlayerState.ViewingAlbum);
        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    // O caso que motivou a task: duas camadas abertas, um Esc, um só a fechar.
    [Test]
    public void EscComDuasCamadas_SoOTopoConsome()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.PushMode(PlayerState.Photographing);

        stateMachine.RequestBack();

        // O InspectionSystem pergunta primeiro e leva com um não.
        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Inspecting));

        // O do topo é que fecha.
        Assert.IsTrue(stateMachine.ConsumeBack(PlayerState.Photographing));

        // E o Esc acabou: mais ninguém o apanha neste frame.
        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Inspecting));
        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Photographing));
    }

    [Test]
    public void EscSoValeUmaVez_MesmoParaOTopo()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.RequestBack();

        Assert.IsTrue(stateMachine.ConsumeBack(PlayerState.Inspecting));
        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Inspecting));
    }

    [Test]
    public void SemEsc_NinguemConsome()
    {
        stateMachine.PushMode(PlayerState.Inspecting);

        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Inspecting));
    }

    [Test]
    public void EscComPilhaVazia_NaoEConsumidoPorNinguem()
    {
        stateMachine.RequestBack();

        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Normal));
        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Inspecting));
    }

    [Test]
    public void Push_EIdempotente()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.PushMode(PlayerState.Inspecting);

        Assert.AreEqual(1, stateMachine.OpenModeCount);

        stateMachine.PopMode(PlayerState.Inspecting);

        Assert.AreEqual(0, stateMachine.OpenModeCount);
    }

    [Test]
    public void PopForaDeOrdem_NaoCorrompeAPilha()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.PushMode(PlayerState.Photographing);

        // Fechar o de baixo primeiro: o topo tem de continuar a ser o de cima.
        stateMachine.PopMode(PlayerState.Inspecting);

        Assert.AreEqual(1, stateMachine.OpenModeCount);
        Assert.AreEqual(PlayerState.Photographing, stateMachine.Current);

        stateMachine.PopMode(PlayerState.Photographing);

        Assert.AreEqual(0, stateMachine.OpenModeCount);
        Assert.AreEqual(PlayerState.Normal, stateMachine.Current);
    }

    [Test]
    public void PopDeQuemNaoEstaNaPilha_NaoFazNada()
    {
        stateMachine.PushMode(PlayerState.Inspecting);

        stateMachine.PopMode(PlayerState.ViewingAlbum);

        Assert.AreEqual(1, stateMachine.OpenModeCount);
        Assert.AreEqual(PlayerState.Inspecting, stateMachine.Current);
    }

    [Test]
    public void IsModeOpen_VeCamadasQueNaoEstaoNoTopo()
    {
        stateMachine.PushMode(PlayerState.Photographing);
        stateMachine.PushMode(PlayerState.PhotoPreview);

        Assert.IsTrue(stateMachine.IsModeOpen(PlayerState.Photographing));
        Assert.IsTrue(stateMachine.IsTopMode(PlayerState.PhotoPreview));
        Assert.IsFalse(stateMachine.IsTopMode(PlayerState.Photographing));
    }

    [Test]
    public void ModoDeRato_TrancaMovimentoELook()
    {
        stateMachine.PushMode(PlayerState.ViewingAlbum);

        Assert.IsTrue(stateMachine.IsCursorFree);
        Assert.IsTrue(stateMachine.IsMovementLocked);
        Assert.IsTrue(stateMachine.IsLookLocked);
    }

    // Esc sem camada aberta larga o rato, e com o rato largo não se joga:
    // é o que impedia a câmara de continuar a rodar com o cursor livre.
    [Test]
    public void EscSemCamadas_LargaORato_EOJogoPara()
    {
        stateMachine.RequestBack();
        stateMachine.ResolveUnconsumedBack();

        Assert.IsTrue(stateMachine.IsCursorFree);
        Assert.IsTrue(stateMachine.IsMovementLocked);
        Assert.IsTrue(stateMachine.IsLookLocked);
    }

    [Test]
    public void EscConsumidoPorUmaCamada_NaoLargaORato()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.RequestBack();

        Assert.IsTrue(stateMachine.ConsumeBack(PlayerState.Inspecting));

        stateMachine.ResolveUnconsumedBack();
        stateMachine.PopMode(PlayerState.Inspecting);

        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    [Test]
    public void AbrirCamada_CancelaOEscDePausa()
    {
        stateMachine.RequestBack();
        stateMachine.ResolveUnconsumedBack();

        Assert.IsTrue(stateMachine.IsCursorFree);

        stateMachine.PushMode(PlayerState.Photographing);
        stateMachine.PopMode(PlayerState.Photographing);

        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    [Test]
    public void EscNaoConsumidoComCamadaAberta_NaoLargaORato()
    {
        // A camada do topo não consumiu o Esc. Isso não pode virar pausa.
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.BeginFrame();
        stateMachine.RequestBack();

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
            "Esc não consumido"));

        stateMachine.ResolveUnconsumedBack();

        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    // Saída de emergência: se o dono do modo do topo não correr o Update (o
    // GameObject onde vive foi desligado, por exemplo), o modo ficava preso e
    // o jogo trancado em silêncio — cursor num estado, movimento noutro.
    [Test]
    public void ModoOrfao_EFechadoAForca_ComAviso()
    {
        stateMachine.PushMode(PlayerState.ViewingAlbum);
        stateMachine.BeginFrame();
        stateMachine.RequestBack();

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
            "ViewingAlbum"));

        stateMachine.ResolveUnconsumedBack();

        Assert.AreEqual(0, stateMachine.OpenModeCount);
        Assert.AreEqual(PlayerState.Normal, stateMachine.Current);
        Assert.IsFalse(stateMachine.IsMovementLocked);
        Assert.IsFalse(stateMachine.IsCursorFree);
    }

    [Test]
    public void ModoOrfao_SoFechaUmaCamadaPorEsc()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.PushMode(PlayerState.Photographing);
        stateMachine.BeginFrame();
        stateMachine.RequestBack();

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
            "Photographing"));

        stateMachine.ResolveUnconsumedBack();

        // A de baixo fica: um Esc fecha uma camada, mesmo à força.
        Assert.AreEqual(1, stateMachine.OpenModeCount);
        Assert.AreEqual(PlayerState.Inspecting, stateMachine.Current);
    }

    [Test]
    public void EscConsumidoNormalmente_NaoDisparaAvisoNemPopExtra()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.PushMode(PlayerState.Photographing);
        stateMachine.RequestBack();

        Assert.IsTrue(stateMachine.ConsumeBack(PlayerState.Photographing));

        // Consumido: nada de aviso e nada de fechar a camada de baixo.
        stateMachine.ResolveUnconsumedBack();

        Assert.AreEqual(2, stateMachine.OpenModeCount);
    }

    // O caso que a primeira versão do pop forçado estragava: o Esc cai no mesmo
    // frame em que a camada abre (LMB+Esc a tirar uma fotografia, E+Esc a
    // inspeccionar). O dono ainda não correu o Update — fechar-lhe a camada
    // aqui deixava o estado interno dele a contradizer a pilha, para sempre.
    [Test]
    public void CamadaAbertaNesteFrame_NaoEFechadaAForca()
    {
        stateMachine.BeginFrame();

        // Mesmo frame: o Esc chega e a camada abre a seguir.
        stateMachine.RequestBack();
        stateMachine.PushMode(PlayerState.PhotoPreview);

        stateMachine.ResolveUnconsumedBack();

        Assert.AreEqual(1, stateMachine.OpenModeCount,
            "A camada acabou de abrir: ninguém lha pode fechar por baixo.");
        Assert.AreEqual(PlayerState.PhotoPreview, stateMachine.Current);
    }

    [Test]
    public void CamadaAbertaNesteFrame_FechaAForcaNoFrameSeguinte()
    {
        stateMachine.BeginFrame();
        stateMachine.RequestBack();
        stateMachine.PushMode(PlayerState.PhotoPreview);
        stateMachine.ResolveUnconsumedBack();

        Assert.AreEqual(1, stateMachine.OpenModeCount);

        // Frame seguinte: se continuar órfã, aí sim.
        stateMachine.BeginFrame();
        stateMachine.RequestBack();

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
            "PhotoPreview"));

        stateMachine.ResolveUnconsumedBack();

        Assert.AreEqual(0, stateMachine.OpenModeCount);
    }

    [Test]
    public void BeginFrame_LimpaOEscDoFrameAnterior()
    {
        stateMachine.PushMode(PlayerState.Inspecting);
        stateMachine.RequestBack();

        stateMachine.BeginFrame();

        Assert.IsFalse(stateMachine.ConsumeBack(PlayerState.Inspecting),
            "Um Esc não consumido não pode transitar para o frame seguinte.");
    }
}
