#ifndef VORONOI3D
#define VORONOI3D

//internal functions:

float RandomFloat(float3 seed) {
	return frac(sin(dot(seed, float3(12.9898, 78.233, 23.141))) * 43758.5453);
}

float3 RandomFloat3(float3 seed) {
	seed = float3(dot(seed, float3(127.1, 311.7, 74.7)),
		dot(seed, float3(269.5, 183.3, 246.1)),
		dot(seed, float3(113.5, 271.9, 124.6)));

	return frac(sin(seed) * 43758.5453123);
}

float SqrMagnitude(float3 vec) {
	return vec.x * vec.x +
		vec.y * vec.y +
		vec.z * vec.z;
}

void Voronoi(float3 pos, out float3 outCellLattice, out float3 outCellPos)
{
	float3 centerLattice = floor(pos);
	float minSqrDist = 100;
	for (int x = -1; x < 2; x++) {
		for (int y = -1; y < 2; y++) {
			for (int z = -1; z < 2; z++) {
				float3 cellLattice = centerLattice + float3(x, y, z);
				float3 cellPos = cellLattice + RandomFloat3(cellLattice);
				float sqrDist = SqrMagnitude(cellPos - pos);
				if (sqrDist < minSqrDist) {
					minSqrDist = sqrDist;
					outCellLattice = cellLattice;
					outCellPos = cellPos;
				}
			}
		}
	}
}
//--------------

void VoronoiRandomScalar_float(float3 Pos, out float Scalar) {
	float3 cellLattice;
	float3 cellPos;
	Voronoi(Pos, cellLattice, cellPos);
	Scalar = RandomFloat(cellLattice);
}

#endif

//!!!!!!!!!!!!!!!
//TODO: fade between multiple materials. idea: unity simple noise node's algorithm