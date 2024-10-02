using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayArea : MonoBehaviour
{
    [SerializeField] protected float _xOffset = 0.1f;
    [SerializeField] protected float bunching = .12f;

    [SerializeField] public List<GameObject> cardData = new List<GameObject>();

    public void ConfigureChildrenPositions() {
        float startingPos = -cardData.Count / 2;

        if (cardData.Count == 1) { startingPos = 0;}
        if (cardData.Count % 2 == 0) { startingPos += 0.5f;}

        for (int i = 0; i < cardData.Count; i++)
        {
            cardData[i].transform.localRotation = Quaternion.identity;
            Vector3 newPosition = new Vector3(startingPos * bunching, 0, 0);
            cardData[i].transform.localPosition = newPosition;
            cardData[i].GetComponent<Card>().SetPosition(newPosition);

            startingPos++;
        }
    }
}
