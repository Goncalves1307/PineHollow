using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Dono único do estado do player, do cursor e do Esc.
//
// Os modos empilham: quem abre uma camada chama PushMode, quem a fecha chama
// PopMode. O topo da pilha manda — é ele que decide se o player anda, se olha
// e se o cursor está preso. Com a pilha vazia vale o estado de locomoção
// (Normal, Sprinting ou Crouching), que o PlayerController define por frame.
//
// O Esc entra por aqui e mais lado nenhum: esta é a única classe do projecto
// que lê a escapeKey. Cada sistema pergunta ConsumeBack(o-seu-modo) e só o do
// topo recebe true — um Esc fecha uma camada, nunca duas.
//
// Corre a -100 para registar o Esc antes de qualquer consumidor: a ordem de
// Update entre MonoBehaviours não é garantida pelo Unity.
[DefaultExecutionOrder(-100)]
public class PlayerStateMachine : MonoBehaviour
{
    private readonly List<PlayerState> modeStack = new List<PlayerState>();

    private PlayerState locomotionState = PlayerState.Normal;

    private bool backRequested;

    // Conta frames da própria máquina. Serve para não fechar à força uma
    // camada que acabou de abrir neste mesmo frame: o dono dela ainda não
    // teve oportunidade de correr o Update.
    private int tick;
    private int lastPushTick = -1;

    // Esc sem camada aberta liberta o cursor, e o clique seguinte volta a
    // prendê-lo. É o que permite recuperar o rato no editor enquanto não
    // houver menu de pausa (FASE 12).
    private bool cursorReleased;

    public PlayerState Current =>
        modeStack.Count > 0
            ? modeStack[modeStack.Count - 1]
            : locomotionState;

    // Quantas camadas estão abertas. Zero significa que o player manda.
    public int OpenModeCount => modeStack.Count;

    // Cursor solto significa que não se joga: nem se anda, nem se olha. É o
    // que impede a câmara de continuar a rodar com o rato livre.
    public bool IsMovementLocked => IsCursorFree || LocksMovement(Current);
    public bool IsLookLocked => IsCursorFree || LocksLook(Current);
    public bool IsCursorFree => cursorReleased || FreesCursor(Current);

    public bool IsModeOpen(PlayerState mode) => modeStack.Contains(mode);

    // O modo no topo é o único que responde ao Esc.
    public bool IsTopMode(PlayerState mode) =>
        modeStack.Count > 0 &&
        modeStack[modeStack.Count - 1] == mode;

    private void Start()
    {
        ApplyCursor();
    }

    private void Update()
    {
        BeginFrame();

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            RequestBack();
        }

        if (cursorReleased &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            cursorReleased = false;
        }

        ApplyCursor();
    }

    private void LateUpdate()
    {
        ResolveUnconsumedBack();

        ApplyCursor();
    }

    // Corre depois de todos os consumidores terem tido a sua vez. Um Esc que
    // ninguém quis, sem camadas abertas, era o pedido de largar o rato.
    public void ResolveUnconsumedBack()
    {
        if (backRequested)
        {
            if (modeStack.Count == 0)
            {
                cursorReleased = true;
            }
            else if (lastPushTick != tick)
            {
                // Saída de emergência. Ninguém quis o Esc apesar de haver uma
                // camada aberta — o dono dela não correu o Update (GameObject
                // desligado, componente desactivado, return antecipado). Sem
                // isto o jogo ficava trancado em silêncio: cursor num estado,
                // movimento noutro, e o Esc sem efeito nenhum.
                //
                // A camada aberta *neste* frame está de fora: o dono só corre
                // no frame seguinte, e fechá-la aqui deixaria o estado dele a
                // contradizer a pilha — que é pior do que o que isto evita.
                PlayerState orfao = modeStack[modeStack.Count - 1];

                Debug.LogWarning(
                    "Esc não consumido com o modo " + orfao + " no topo: " +
                    "ninguém o fechou. A fechar à força — o dono deste modo " +
                    "não está a correr o Update."
                );

                modeStack.RemoveAt(modeStack.Count - 1);

                ApplyCursor();
            }
        }

        backRequested = false;
    }

    // Define a locomoção enquanto não houver camadas abertas. Só aceita os
    // três estados de locomoção — os outros entram pela pilha.
    public void SetLocomotion(PlayerState state)
    {
        if (state != PlayerState.Normal &&
            state != PlayerState.Sprinting &&
            state != PlayerState.Crouching)
        {
            Debug.LogError(
                "SetLocomotion só aceita Normal, Sprinting ou Crouching. " +
                "Recebeu: " + state
            );

            return;
        }

        locomotionState = state;
    }

    public void PushMode(PlayerState mode)
    {
        // Idempotente: um Update que empurre duas vezes não corrompe a pilha.
        if (modeStack.Contains(mode))
            return;

        modeStack.Add(mode);

        lastPushTick = tick;

        // Abrir uma camada cancela o Esc de pausa.
        cursorReleased = false;

        ApplyCursor();
    }

    public void PopMode(PlayerState mode)
    {
        int index = modeStack.LastIndexOf(mode);

        if (index < 0)
            return;

        modeStack.RemoveAt(index);

        ApplyCursor();
    }

    // Abre o frame da máquina. Público para os testes poderem simular a
    // passagem de um frame sem correr o ciclo de vida do Unity.
    public void BeginFrame()
    {
        tick++;
        backRequested = false;
    }

    public void RequestBack()
    {
        backRequested = true;
    }

    // Devolve true uma só vez por Esc, e apenas a quem está no topo da pilha.
    public bool ConsumeBack(PlayerState mode)
    {
        if (!backRequested || !IsTopMode(mode))
            return false;

        backRequested = false;

        return true;
    }

    private void ApplyCursor()
    {
        bool free = IsCursorFree;

        Cursor.lockState = free
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = free;
    }

    private static bool LocksMovement(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Normal:
            case PlayerState.Sprinting:
            case PlayerState.Crouching:
            case PlayerState.Interacting:
                return false;

            default:
                return true;
        }
    }

    private static bool LocksLook(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Normal:
            case PlayerState.Sprinting:
            case PlayerState.Crouching:
            case PlayerState.Interacting:
                return false;

            // Em modo fotografia o player não anda mas continua a apontar.
            case PlayerState.Photographing:
                return false;

            default:
                return true;
        }
    }

    private static bool FreesCursor(PlayerState state)
    {
        switch (state)
        {
            // As únicas camadas que se usam com o rato.
            case PlayerState.ViewingAlbum:
            case PlayerState.ViewingPhoto:

            // Alinhar uma fotografia sobre a outra é arrastar com o rato:
            // sem o cursor livre não há como lá chegar.
            case PlayerState.ComparingPhotos:
                return true;

            default:
                return false;
        }
    }
}
