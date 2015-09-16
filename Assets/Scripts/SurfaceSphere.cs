using UnityEngine;
using System.Collections;

public class SurfaceSphere : MonoBehaviour {
	public Material material;
	// Use this for initialization

	public void Init(){
	}
	public void Create (Vector3[] vertices, int nbLong, int nbLat, Color color) {

		#region Normales		
		Vector3[] normales = new Vector3[vertices.Length];
		Color[] colors = new Color[vertices.Length];
		for( int n = 0; n < vertices.Length; n++ ){
			if(vertices[n]!=Vector3.zero){
				normales[n] = vertices[n].normalized;
				colors[n] = color;
			}
		}
		#endregion

		#region UVs
		Vector2[] uvs = new Vector2[vertices.Length];

		for( int lat = 0; lat < nbLat; lat++ )
			for( int lon = 0; lon <= nbLong; lon++ )
				uvs[lon + lat * (nbLong + 1)] = new Vector2( (float)lon / nbLong, 1f - (float)(lat+1) / (nbLat+1) );
		#endregion

		
		#region Triangles
		int nbFaces = vertices.Length;
		int nbTriangles = nbFaces * 2;
		int nbIndexes = nbTriangles * 3;
		int[] triangles = new int[ nbIndexes ];
		
		//Top Cap
		int i = 0;
		for( int lon = 0; lon < nbLong; lon++ )
		{
				triangles[i++] = lon+2;
				triangles[i++] = lon+1;
				triangles[i++] = 0;

		}

		//Middle
		for( int lat = 0; lat < nbLat - 1; lat++ )
		{
			for( int lon = 0; lon < nbLong; lon++ )
			{
				int current = lon + lat * (nbLong + 1);
				int next = current + nbLong + 1;

				triangles[i++] = current;
				triangles[i++] = current + 1;
				triangles[i++] = next + 1;

				
				triangles[i++] = current;
				triangles[i++] = next + 1;
				triangles[i++] = next;

			}
		}

		//Bottom Cap
		for( int lon = 0; lon < nbLong; lon++ )
		{
				triangles[i++] = vertices.Length - 1;
				triangles[i++] = vertices.Length - (lon+1) - 1;
				triangles[i++] = vertices.Length - (lon+2) - 1;

		}
		#endregion

		MeshRenderer mr = GetComponent<MeshRenderer> ();
		mr.material = new Material (material.shader);
		GetComponent<Renderer> ().enabled = true;

		Mesh mesh = new Mesh ();

		mesh.vertices = vertices;
		mesh.normals = normales;
		mesh.uv = uvs;
		mesh.colors = colors;
		mesh.triangles = triangles;
		mesh.RecalculateBounds ();

		GetComponent<MeshFilter> ().sharedMesh = mesh;

	}
	

}
