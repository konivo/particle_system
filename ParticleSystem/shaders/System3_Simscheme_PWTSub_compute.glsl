#version 440
////////////////////////////////////////////////////////////////////////////////
//types//
subroutine vec4 _Map(vec4 p);


////////////////////////////////////////////////////////////////////////////////
//uniforms//


////////////////////////////////////////////////////////////////////////////////
//constants//
const int c_CenterCount = 30, c_CenterParamCount = 9;

////////////////////////////////////////////////////////////////////////////////
//buffer//
buffer MapParameters
{
	float[] a;
};

////////////////////////////////////////////////////////////////////////////////
//subroutines//

///////////////
//subroutine(Map)
vec4 _Lorenz(vec4 p)
{
	return vec4(
		a[0] * (p.y - p.x),
		p.x * (a[1] - p.z) - p.y,
		p.x * p.y - p.z * a[2],
		0);
} 

///////////////
//subroutine(Map)
vec4 _SpiralBMap(vec4 p)
{
	vec4 o = vec4(0, 0, 0, 0);
	for(int i = 0; i < c_CenterCount * c_CenterParamCount; i+= c_CenterParamCount)
	{
		vec4 center = vec4(a[i], a[i + 1], a[i + 2], 0);
		//SpiralBMapInternal(a[i + 3], a[i + 4], center, input, output);
	}

	o /= c_CenterCount * 3;
	return o;
}
/*
void SpiralBMapInternal(float k, float acc, inout vec4 center, inout vec4 input, out vec4 output)
{
	vec4 tmp = input - center;
	float dist = length(tmp);

	for(int i = 0; i < 3; i++)
	{
		//if(float.IsNaN(tmp.X) || float.IsNaN(tmp.Y))
			//break;

		vec4 d = 
			normalize(vec4(
				k * tmp.y,
				-k * tmp.x,
				1,
				0
			));

		d *= 1f/max(sqrt(dist), 0.1f);
		output += acc * d;
		tmp = vec4(tmp.Z, tmp.X, tmp.Y,0);
		output = vec4(output.zxy, 0);
	}
}
*/
