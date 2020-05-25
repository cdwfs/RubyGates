using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class GateInputsN : MonoBehaviour, IDeclareReferencedPrefabs
{
    public Wire wirePrefab;
    public Transform attachTransform;
    public Gate[] inputNodes;
    
    void OnDrawGizmos()
    {
        // Draw lines from this node's inputs' output attach points to this node's input attach point.
        Gizmos.color = Color.yellow;
        foreach(var inputNode in inputNodes)
        {
            Gizmos.DrawLine(inputNode.outputAttachTransform.position, attachTransform.position);
        }

        // Little tip on each line
        Gizmos.color = Color.cyan;
        foreach (var inputNode in inputNodes)
        {
            Gizmos.DrawLine(
                Vector3.Lerp(inputNode.outputAttachTransform.position, attachTransform.position, 0.9f),
                attachTransform.position);
        }
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (wirePrefab != null)
            referencedPrefabs.Add(wirePrefab.gameObject);
    }
}