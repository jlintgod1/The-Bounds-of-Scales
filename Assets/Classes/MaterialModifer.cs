using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialModifer : MonoBehaviour
{
    public Material Material;
    public string ParameterName;
    public float ParameterValue;

    // Update is called once per frame
    void Update()
    {
        Material.SetFloat(ParameterName, ParameterValue);
    }
}
