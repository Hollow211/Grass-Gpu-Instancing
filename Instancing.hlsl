#ifndef INSTANCING_INCLUDED
#define INSTANCING_INCLUDED

struct DataLayout
{
	float4x4 m;
	float2 world_UV;
	float3 normal;
	float colorIndex;
};

StructuredBuffer<DataLayout> _InstanceDataBuffer;
float3 Normal;
float2 world_UV;
float colorIndex;

void GetInstanceData()
{
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	unity_ObjectToWorld = mul(unity_ObjectToWorld,_InstanceDataBuffer[unity_InstanceID].m);

	Normal = _InstanceDataBuffer[unity_InstanceID].normal;

	world_UV = _InstanceDataBuffer[unity_InstanceID].world_UV;

	colorIndex = _InstanceDataBuffer[unity_InstanceID].colorIndex;
	// Inverse transform matrix
		float3x3 w2oRotation;
		w2oRotation[0] = unity_ObjectToWorld[1].yzx * unity_ObjectToWorld[2].zxy - unity_ObjectToWorld[1].zxy * unity_ObjectToWorld[2].yzx;
		w2oRotation[1] = unity_ObjectToWorld[0].zxy * unity_ObjectToWorld[2].yzx - unity_ObjectToWorld[0].yzx * unity_ObjectToWorld[2].zxy;
		w2oRotation[2] = unity_ObjectToWorld[0].yzx * unity_ObjectToWorld[1].zxy - unity_ObjectToWorld[0].zxy * unity_ObjectToWorld[1].yzx;

		float det = dot(unity_ObjectToWorld[0].xyz, w2oRotation[0]);
		w2oRotation = transpose(w2oRotation);
		w2oRotation *= rcp(det);
		float3 w2oPosition = mul(w2oRotation, -unity_ObjectToWorld._14_24_34);

		unity_WorldToObject._11_21_31_41 = float4(w2oRotation._11_21_31, 0.0f);
		unity_WorldToObject._12_22_32_42 = float4(w2oRotation._12_22_32, 0.0f);
		unity_WorldToObject._13_23_33_43 = float4(w2oRotation._13_23_33, 0.0f);
		unity_WorldToObject._14_24_34_44 = float4(w2oPosition, 1.0f);
	#endif
}	




// Dummy function
void Instancing_float(float3 Position, out float3 Out,out float3 NormalOutput,out float2 World_UV_OutPut, out float ColorIndex){
	Out = Position;
	NormalOutput = Normal;
	World_UV_OutPut = world_UV;
	ColorIndex = colorIndex;
}

#endif