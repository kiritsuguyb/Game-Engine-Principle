using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MyShaderGUI : ShaderGUI
{
    MaterialEditor editor;
    MaterialProperty[] properties;
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        editor = materialEditor;
        this.properties = properties;
        base.OnGUI(materialEditor, properties);
        defineTexMacro();

    }
    void defineTexMacro()
    {
        setPropertyKeyword("_MainTex");
        setPropertyKeyword("_NormalTex");
        setPropertyKeyword("_DetailNormalTex");
    }
    void setPropertyKeyword(string name)
    {
        var map = FindProperty(name);
        Material mat = editor.target as Material;
        if (map.textureValue)
            mat.EnableKeyword(name+"_ON");
        else mat.DisableKeyword(name+"_ON");
    }
    MaterialProperty FindProperty(string name)
    {
        return FindProperty(name, properties);
    }
}
