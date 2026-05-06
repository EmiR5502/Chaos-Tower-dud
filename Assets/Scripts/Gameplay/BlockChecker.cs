using UnityEngine;
using ChaosTower.Managers;

public class BlockChecker : MonoBehaviour
{ 
    void Update() 
    { 
        if (transform.position.y < -5f) 
        { GameManager.Instance.EndGame(); 
        } 
    }
}