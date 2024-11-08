Shader "Custom/AlwaysOnTop"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" }
        Pass
        {
            // Ensure the shader ignores the depth buffer so it's always rendered on top
            ZWrite Off
            ZTest Always
            Color [_Color]
        }
    }
}
