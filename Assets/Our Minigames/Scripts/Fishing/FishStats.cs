using UnityEngine;

public class FishStats : MonoBehaviour
{
    public enum FishType {Sardine, Orange_Fish, Smallmouth_Buffalo, Carp, Red_Fish, Yellow_Tuna, humanfishthing};
    public FishType type;
    public float speed;
    public float weight;
    public float resistance;
    public float baitChance;
    public float multiplier;
    public float spawnChance;

    public bool tutorialFish = false;

    // Start is called before the first frame update
    void Awake()
    {
        speed = Random.Range(1f, 5f);

        if (!tutorialFish)
        {
            switch (type)
            {
                case FishType.Sardine:
                    weight = Random.Range(.25f, .5f);
                    resistance = Mathf.Round(Random.Range(3, 6));
                    baitChance = (.5f - (weight / resistance)) / .5f;
                    multiplier = 1.5f;
                    spawnChance = .60f;
                    break;
                case FishType.Orange_Fish:
                    weight = Random.Range(.28f, 1.25f);
                    resistance = Mathf.Round(Random.Range(4, 5));
                    baitChance = (1.25f - (weight / resistance)) / 1.25f;
                    multiplier = 1.25f;
                    spawnChance = .40f;
                    break;
                case FishType.Smallmouth_Buffalo:
                    weight = Random.Range(.27f, 2.1f);
                    resistance = Mathf.Round(Random.Range(5, 7));
                    baitChance = (2.1f - (weight / resistance)) / 2.1f;
                    multiplier = 1.4f;
                    spawnChance = .35f;
                    break;
                case FishType.Carp:
                    weight = Random.Range(.33f, 2.5f);
                    resistance = Mathf.Round(Random.Range(4, 8));
                    baitChance = (2.5f - (weight / resistance)) / 2.5f;
                    multiplier = 1.6f;
                    spawnChance = .25f;
                    break;
                case FishType.Red_Fish:
                    weight = Random.Range(1.8f, 2.8f);
                    resistance = Mathf.Round(Random.Range(6, 9));
                    baitChance = (2.8f - (weight / resistance)) / 2.8f;
                    multiplier = 2f;
                    spawnChance = .15f;
                    break;
                case FishType.Yellow_Tuna:
                    weight = Random.Range(3.6f, 5.2f);
                    resistance = Mathf.Round(Random.Range(10, 12));
                    baitChance = (5.2f - (weight / resistance)) / 5.2f;
                    multiplier = 6f;
                    spawnChance = .05f;
                    break;
                case FishType.humanfishthing:
                    weight = 1;
                    resistance = 40f;
                    baitChance = .004f;
                    multiplier = 10f;
                    spawnChance = .001f;
                    break;
            }
        }
    }
}
