using UnityEngine;
using NaughtyAttributes;

[AddComponentMenu("Eclipse/BodyRotate", order: 0)]
public class BodyRotate : MonoBehaviour
{
    public bool forceFaceSomething = false;
    [ShowIf("forceFaceSomething")] public Transform forceFaceTarget;

    [HideIf("forceFaceSomething")] public float speed = 0;

    // Start is called before the first frame update
    public void Start()
    {
        if (forceFaceSomething)
        {
            transform.LookAt(forceFaceTarget);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (forceFaceSomething)
        {
            transform.LookAt(forceFaceTarget);
        }
        else
        {
            transform.Rotate(Vector3.up, speed * Time.deltaTime);
        }
    }
}