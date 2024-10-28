using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayArea : MonoBehaviour
{
    [SerializeField] protected float _xOffset = 0.1f;
    [SerializeField] protected float bunching = .12f;

    public void ConfigureChildrenPositions(List<GameObject> cards) {
        Debug.Log(cards.Count);
        float startingPos = -cards.Count / 2;

        if (cards.Count == 1) { startingPos = 0;}
        if (cards.Count % 2 == 0) { startingPos += 0.5f;}

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.localRotation = Quaternion.identity;
            Vector3 newPosition = new Vector3(startingPos * bunching, 0, 0);
            cards[i].transform.localPosition = newPosition;
            cards[i].GetComponent<Card>().SetPosition(newPosition);

            startingPos++;
        }
    }
}
