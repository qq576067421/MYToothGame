#ifndef linear_gamma_convert
#define linear_gamma_convert

	float3 LinearToSRGB(float3 c)
	{
		float3 sRGBLo = c * 12.92;
		float3 sRGBHi = (pow(abs(c), float3(1.0/2.4, 1.0/2.4, 1.0/2.4)) * 1.055) - 0.055;
		float3 sRGB   = (c <= 0.0031308) ? sRGBLo : sRGBHi;
		return sRGB;
	}
	
	half3 SRGBToLinear(half3 c)
	{
		half3 linearRGBLo = c / 12.92;
		half3 linearRGBHi = pow(abs((c + 0.055) / 1.055), 2.4);
		half3 linearRGB = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
		return linearRGB;
    }
#endif