using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadBodyPieceManager
{
    List<DeadBodyPiece> deadBodyPieces = new List<DeadBodyPiece>();

    public static DeadBodyPieceManager Instance { private set; get; }

    public DeadBodyPieceManager()
    {
        Instance = this;
    }

    public void RegisterDeadBodyPiece(DeadBodyPiece piece)
    {
        deadBodyPieces.Add(piece);
    }

    public void UnregisterDeadBodyPiece(DeadBodyPiece piece)
    {
        deadBodyPieces.Remove(piece);
    }

    public void FadeOutAll()
    {
        // Make the dead body pieces disappear so they're not lying around the whole game!
        foreach (DeadBodyPiece piece in deadBodyPieces)
        {
            piece.TriggerFadeOut();
        }
    }
}
