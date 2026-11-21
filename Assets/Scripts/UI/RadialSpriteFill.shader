
Shader "UI/RadialSpriteFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StartAngle ("Start Angle", Float) = -35
        _EndAngle ("End Angle", Float) = 355
        _Progress ("Progress", Range(0,1)) = 1
        _SegmentCount ("Segment Count", Float) = 30
        _SegmentGap ("Segment Gap", Float) = 3
        _SegmentColor ("Segment Line Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        // configure the shader for transparent UI rendering
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha // enable transparency
        Cull Off // no culling
        ZWrite Off // prevent from writing to depth buffer

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // data received from the UI Image mesh
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            // data passed from the vertex shader to the fragment shader
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _StartAngle;
            float _EndAngle;
            float _Progress;
            float _SegmentCount;
            float _SegmentGap;
            fixed4 _SegmentColor;

            // vertex shader that converts the UI Image vertices to screen coordinates
            // and passes the texture coordinates and colour to the fragment shader
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // convert vertex position to clop space
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            // fragment shader that determines which parts of the sprite should be visible
            // based on the starting angle, ending angle, and progress
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color; // get the original sprite colour and transparency

                if (col.a <= 0.001) // hide pixels outside the original sprite.
                {
                    discard;
                }

                float2 centeredUV = i.uv - float2(0.5, 0.5);

                float angle = atan2(centeredUV.y, centeredUV.x) * 57.29578;
                angle = (angle + 360.0) % 360.0;

                float start = (_StartAngle + 360.0) % 360.0;
                float end = (_EndAngle + 360.0) % 360.0;

                float totalAngle = (end - start + 360.0) % 360.0;
                float currentAngle = (start - angle + 360.0) % 360.0;
                float visibleAngle = totalAngle * saturate(_Progress);

                // hide segment lines for used up ammo
                if (currentAngle > visibleAngle || currentAngle > totalAngle)
                {
                    discard;
                }

                float segmentAngle = totalAngle / max(_SegmentCount, 1.0); // calculate the angle for each individual segment
                float segmentIndex = floor(currentAngle / segmentAngle);
                float segmentLocalAngle = fmod(currentAngle, segmentAngle);
                float halfGap = _SegmentGap * 0.5;

                // determine which pixels are inside the segment gap (the segment line)
                bool inSegmentGap =
                    (segmentIndex > 0 &&
                     segmentLocalAngle < halfGap) ||
                    (segmentIndex < _SegmentCount - 1 &&
                     segmentLocalAngle > segmentAngle - halfGap);

                if (inSegmentGap)
                {
                    fixed4 segmentCol = _SegmentColor * i.color;
                    segmentCol.a *= col.a;
                    return segmentCol;
                }
                return col;
            }
            ENDCG
        }
    }
}
