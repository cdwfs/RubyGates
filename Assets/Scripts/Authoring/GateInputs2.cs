using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class GateInputs2 : MonoBehaviour, IDeclareReferencedPrefabs
{
    public Wire wirePrefab;
    public Transform attachTransformL;
    public Transform attachTransformR;
    public Gate inputNodeL;
    public Gate inputNodeR;
    
    void OnDrawGizmos()
    {
        // Draw lines from this node's inputs' output attach points to this node's input attach point.
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(inputNodeL.outputAttachTransform.position, attachTransformL.position);
        Gizmos.DrawLine(inputNodeR.outputAttachTransform.position, attachTransformR.position);

        // Little tip on each line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            Vector3.Lerp(inputNodeL.outputAttachTransform.position, attachTransformL.position, 0.9f),
            attachTransformL.position);
        Gizmos.DrawLine(
            Vector3.Lerp(inputNodeR.outputAttachTransform.position, attachTransformR.position, 0.9f),
            attachTransformR.position);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (wirePrefab != null)
            referencedPrefabs.Add(wirePrefab.gameObject);
    }
}
