using SharpDX;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodeWalker.GameFiles;
using CodeWalker.Utils;
namespace CodeWalker
{
    class Exporter
    {
        Vector3[] GetMeshPositions(DrawableGeometry Mesh)
        {
            Vector3[] Positions = new Vector3[Mesh.VerticesCount];
            for (int i = 0; i < Mesh.VerticesCount; i++)
            {
                int StartIndex = Mesh.VertexStride* i;
                Positions[i] = new Vector3(BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex), BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex + 4), BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex + 8));
            }
            return Positions;
        }
        Vector3[] GetMeshNormals(DrawableGeometry Mesh)
        {
            Vector3[] Normals = new Vector3[Mesh.VerticesCount];
            int Offset = 0;
            switch (Mesh.VertexData.VertexType)
            {
                case VertexType.Default: //PNCT
                case VertexType.DefaultEx: //PNCTX
                case VertexType.PNCCT:
                case VertexType.PNCCTTTT:
                case VertexType.PNCTTTX:
                case VertexType.PNCTTX:
                case VertexType.PNCCTTX:
                case VertexType.PNCCTTTX:
                    Offset = 12;
                    break;
                default:
                    throw new Exception();
            }
            for (int i = 0; i < Mesh.VerticesCount; i++)
            {
                int StartIndex = Mesh.VertexStride * i + Offset;
                Normals[i] = new Vector3(BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex), BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex + 4), BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex + 8));
            }
            return Normals;
        }
        Vector3[] GetMeshTangents(DrawableGeometry Mesh)
        {
            int Offset = 0;
            switch (Mesh.VertexData.VertexType)
            {
                case VertexType.Default: //PNCT
                case VertexType.PNCCT:
                case VertexType.PNCCTTTT:
                    return null;
                case VertexType.DefaultEx: //PNCTX
                    Offset = 36;
                    break;
                case VertexType.PNCTTX:
                    Offset = 44;
                    break;
                case VertexType.PNCTTTX:
                    Offset = 52;
                    break;
                case VertexType.PNCCTTX:
                    Offset = 48;
                    break;
                case VertexType.PNCCTTTX:
                    Offset = 56;
                    break;
                default:
                    throw new Exception();
            }
            Vector3[] Tangents = new Vector3[Mesh.VerticesCount];
            for (int i = 0; i < Mesh.VerticesCount; i++)
            {
                int StartIndex = Mesh.VertexStride * i + Offset;
                Tangents[i] = new Vector3(BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex), BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex + 4), BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex + 8));
            }
            return Tangents;
        }
        Vector2[] GetMeshUVs(DrawableGeometry Mesh)
        {
            Vector2[] UVs = new Vector2[Mesh.VerticesCount];
            int Offset = 0;
            switch (Mesh.VertexData.VertexType)
            {
                case VertexType.Default: //PNCT
                case VertexType.DefaultEx: //PNCTX
                case VertexType.PNCTTTX:
                case VertexType.PNCTTX:
                    Offset = 28;
                    break;
                case VertexType.PNCCT:
                case VertexType.PNCCTTTT:
                case VertexType.PNCCTTX:
                case VertexType.PNCCTTTX:
                    Offset = 32;
                    break;
                default:
                    throw new Exception();
            }
            for (int i = 0; i < Mesh.VerticesCount; i++)
            {
                int StartIndex = Mesh.VertexStride * i + Offset;
                UVs[i] = new Vector2(BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex), BitConverter.ToSingle(Mesh.VertexData.VertexBytes, StartIndex + 4));
            }
            return UVs;
        }
        public Exporter(string name, ResourcePointerList64<DrawableModel> models, GameFileCache Cache)
        {
            using (StreamWriter FBXwriter = new StreamWriter("FBX/" + name + ".fbx"))
            {
                var timestamp = DateTime.Now;
                int BaseId = 10000;

                StringBuilder fbx = new StringBuilder();
                StringBuilder ob = new StringBuilder(); //Objects builder
                StringBuilder cb = new StringBuilder(); //Connections builder
                StringBuilder mb = new StringBuilder(); //Materials builder to get texture count in advance
                StringBuilder cb2 = new StringBuilder(); //and keep connections ordered
                cb.Append("\n}\n");//Objects end
                cb.Append("\nConnections:  {");
                List<DrawableGeometry> Geoms = new List<DrawableGeometry>();
                List<ShaderFX> Shaders = new List<ShaderFX>();
                List<Texture> Textures = new List<Texture>();
                //Models
                for (int mi = 0; mi < models.data_items.Length; mi++)
                {
                    var model = models.data_items[mi];
                    //SubMesh & Materials
                    foreach (var geom in model.Geometries.data_items)
                    {
                        if ((geom.Shader != null) && (geom.Shader.ParametersList != null) && (geom.Shader.ParametersList.Hashes != null))
                        {
                            Geoms.Add(geom);
                            Shaders.Add(geom.Shader);
                            var gname = "Geom" + Geoms.Count;
                            //创建节点
                            ob.AppendFormat("\n\tModel: 1{0}, \"Model::{1}\", \"Mesh\" {{", BaseId + Geoms.Count, gname);
                            ob.Append("\n\t\tVersion: 232");
                            ob.Append("\n\t\tProperties70:  {");
                            ob.Append("\n\t\t\tP: \"InheritType\", \"enum\", \"\", \"\",1");
                            ob.Append("\n\t\t\tP: \"ScalingMax\", \"Vector3D\", \"Vector\", \"\",0,0,0");
                            ob.Append("\n\t\t\tP: \"DefaultAttributeIndex\", \"int\", \"Integer\", \"\",0");
                            ob.AppendFormat("\n\t\t\tP: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",{0},{1},{2}", 0, 0, 0);
                            ob.AppendFormat("\n\t\t\tP: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",{0},{1},{2}", 0, 0, 0);//handedness is switched in quat
                            ob.AppendFormat("\n\t\t\tP: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",{0},{1},{2}", 1, 1, 1);
                            ob.Append("\n\t\t}");
                            ob.Append("\n\t\tShading: T");
                            ob.Append("\n\t\tCulling: \"CullingOff\"\n\t}");

                            //把节点挂在根节点上
                            cb.AppendFormat("\n\n\t;Model::{0}, Model::RootNode", gname);
                            cb.AppendFormat("\n\tC: \"OO\",1{0},0", BaseId + Geoms.Count);

                            //把几何体挂在节点上
                            cb2.AppendFormat("\n\n\t;Geometry::, Model::{0}", gname);
                            cb2.AppendFormat("\n\tC: \"OO\",3{0},1{1}", BaseId + Geoms.Count, BaseId + Geoms.Count);
                            //把材质挂在节点上
                            cb2.AppendFormat("\n\n\t;Material::, Model::{0}", gname);
                            cb2.AppendFormat("\n\tC: \"OO\",6{0},1{1}", BaseId + Shaders.Count, BaseId + Geoms.Count);

                            var pl = geom.Shader.ParametersList;
                            var h = pl.Hashes;
                            var p = pl.Parameters;
                            for (int ip = 0; ip < h.Length; ip++)
                            {
                                var hash = pl.Hashes[ip];
                                var parm = pl.Parameters[ip];
                                var tex = parm.Data as TextureBase;
                                if (tex != null)
                                {
                                    var t = tex as Texture;
                                    if (t == null)
                                    {
                                        YtdFile file = Cache.TryGetTextureDictForTexture(tex.NameHash);
                                        if (file != null)
                                            t = file.TextureDict.Lookup(tex.NameHash);
                                    }
                                    var tstr = tex.Name.Trim();
                                    if (t != null)
                                    {
                                        Textures.Add(t);
                                        cb2.AppendFormat("\n\n\t;Texture::, Material::{0}", geom.Shader.Name);
                                        cb2.AppendFormat("\n\tC: \"OP\",7{0},6{1}, ", BaseId + Textures.Count, BaseId + Shaders.Count);
                                        switch (hash.ToString().Trim())
                                        {
                                            case "DiffuseSampler":
                                                cb2.Append("\"DiffuseColor\"");
                                                break;
                                            case "BumpSampler":
                                                cb2.Append("\"NormalMap\"");
                                                break;
                                            case "SpecSampler":
                                                cb2.Append("\"SpecularColor\"");
                                                break;
                                            case "DetailSampler":
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }



                fbx.Append("; FBX 7.1.0 project file");
                fbx.Append("\nFBXHeaderExtension:  {\n\tFBXHeaderVersion: 1003\n\tFBXVersion: 7100\n\tCreationTimeStamp:  {\n\t\tVersion: 1000");
                fbx.Append("\n\t\tYear: " + timestamp.Year);
                fbx.Append("\n\t\tMonth: " + timestamp.Month);
                fbx.Append("\n\t\tDay: " + timestamp.Day);
                fbx.Append("\n\t\tHour: " + timestamp.Hour);
                fbx.Append("\n\t\tMinute: " + timestamp.Minute);
                fbx.Append("\n\t\tSecond: " + timestamp.Second);
                fbx.Append("\n\t\tMillisecond: " + timestamp.Millisecond);
                fbx.Append("\n\t}\n\tCreator: \"Unity Studio by Chipicao\"\n}\n");

                fbx.Append("\nGlobalSettings:  {");
                fbx.Append("\n\tVersion: 1000");
                fbx.Append("\n\tProperties70:  {");
                fbx.Append("\n\t\tP: \"UpAxis\", \"int\", \"Integer\", \"\",1");
                fbx.Append("\n\t\tP: \"UpAxisSign\", \"int\", \"Integer\", \"\",1");
                fbx.Append("\n\t\tP: \"FrontAxis\", \"int\", \"Integer\", \"\",2");
                fbx.Append("\n\t\tP: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");
                fbx.Append("\n\t\tP: \"CoordAxis\", \"int\", \"Integer\", \"\",0");
                fbx.Append("\n\t\tP: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
                fbx.Append("\n\t\tP: \"OriginalUpAxis\", \"int\", \"Integer\", \"\",1");
                fbx.Append("\n\t\tP: \"OriginalUpAxisSign\", \"int\", \"Integer\", \"\",1");
                fbx.AppendFormat("\n\t\tP: \"UnitScaleFactor\", \"double\", \"Number\", \"\",1");
                fbx.Append("\n\t\tP: \"OriginalUnitScaleFactor\", \"double\", \"Number\", \"\",1.0");
                fbx.Append("\n\t}\n}\n");

                fbx.Append("\nDocuments:  {");
                fbx.Append("\n\tCount: 1");
                fbx.Append("\n\tDocument: 1234567890, \"\", \"Scene\" {");
                fbx.Append("\n\t\tProperties70:  {");
                fbx.Append("\n\t\t\tP: \"SourceObject\", \"object\", \"\", \"\"");
                fbx.Append("\n\t\t\tP: \"ActiveAnimStackName\", \"KString\", \"\", \"\", \"\"");
                fbx.Append("\n\t\t}");
                fbx.Append("\n\t\tRootNode: 0");
                fbx.Append("\n\t}\n}\n");
                fbx.Append("\nReferences:  {\n}\n");

                fbx.Append("\nDefinitions:  {");
                fbx.Append("\n\tVersion: 100");
        //      fbx.AppendFormat("\n\tCount: {0}", 1 + 2 * GameObjects.Count + Materials.Count + 2 * Textures.Count + ((bool)Properties.Settings.Default["exportDeformers"] ? Skins.Count + DeformerCount + Skins.Count + 1 : 0));

                fbx.Append("\n\tObjectType: \"GlobalSettings\" {");
                fbx.Append("\n\t\tCount: 1");
                fbx.Append("\n\t}");

                fbx.Append("\n\tObjectType: \"Model\" {");
                fbx.AppendFormat("\n\t\tCount: {0}", 1);
                fbx.Append("\n\t}");

                fbx.Append("\n\tObjectType: \"Geometry\" {");
                fbx.AppendFormat("\n\t\tCount: {0}", Geoms.Count);
                fbx.Append("\n\t}");

                fbx.Append("\n\tObjectType: \"Material\" {");
                fbx.AppendFormat("\n\t\tCount: {0}", Shaders.Count);
                fbx.Append("\n\t}");

                fbx.Append("\n\tObjectType: \"Texture\" {");
                fbx.AppendFormat("\n\t\tCount: {0}", Textures.Count);
                fbx.Append("\n\t}");


                fbx.Append("\n}\n");
                fbx.Append("\nObjects:  {");

                FBXwriter.Write(fbx);
                fbx.Clear();







                for ( int i = 0; i < Shaders.Count; i++)
                {
                    ShaderFX Shader = Shaders[i];
                    mb.AppendFormat("\n\tMaterial: 6{0}, \"Material::{1}\", \"\" {{", BaseId + i + 1, Shader.Name);
                    mb.Append("\n\t\tVersion: 102");
                    mb.Append("\n\t\tShadingModel: \"phong\"");
                    mb.Append("\n\t\tMultiLayer: 0");
                    mb.Append("\n\t\tProperties70:  {");
                    mb.Append("\n\t\t\tP: \"ShadingModel\", \"KString\", \"\", \"\", \"phong\"");

                    //mb.Append("\n\t\t\tP: \"SpecularFactor\", \"Number\", \"\", \"A\",0");
                    mb.Append("\n\t\t}");
                    mb.Append("\n\t}");

                }
                for (int i = 0; i < Geoms.Count; i++)
                {
                    MeshFBX(Geoms[i], BaseId + i + 1, ob);

                    //write data 8MB at a time
                    if (ob.Length > (8 * 0x100000))
                    { FBXwriter.Write(ob); ob.Clear(); }
                }
                for(int i = 0; i < Textures.Count; i++)
                {
                    Texture t = Textures[i];
                    //TODO check texture type and set path accordingly; eg. CubeMap, Texture3D
                    string texFilename = Path.GetFullPath("FBX/" + t.Name + ".png");

                    byte[] bytes = DDSIO.GetPixels(t, 0);
                    FileStream stream = new FileStream(texFilename, FileMode.Create);
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Interlace = PngInterlaceOption.On;
                    encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(t.Width, t.Height, 96, 96, PixelFormats.Bgra32, null, bytes, t.Width * 4)));
                    encoder.Save(stream);
                    stream.Close();


                //  File.WriteAllBytes(texFilename, DDSIO.GetDDSFile(t));



                    ob.AppendFormat("\n\tTexture: 7{0}, \"Texture::{1}\", \"\" {{", BaseId + i + 1, t.Name);
                    ob.Append("\n\t\tType: \"TextureVideoClip\"");
                    ob.Append("\n\t\tVersion: 202");
                    ob.AppendFormat("\n\t\tTextureName: \"Texture::{0}\"", t.Name);
                    ob.Append("\n\t\tProperties70:  {");
                    ob.Append("\n\t\t\tP: \"UVSet\", \"KString\", \"\", \"\", \"UVChannel_0\"");
                    ob.Append("\n\t\t\tP: \"UseMaterial\", \"bool\", \"\", \"\",1");
                    ob.Append("\n\t\t}");
                    ob.AppendFormat("\n\t\tMedia: \"Video::{0}\"", t.Name);
                    ob.AppendFormat("\n\t\tFileName: \"{0}\"", texFilename);
                    ob.AppendFormat("\n\t\tRelativeFilename: \"{0}\"", texFilename);
                    ob.Append("\n\t}");
                }

                ob.Append(mb); mb.Clear();
                cb.Append(cb2); cb2.Clear();

                FBXwriter.Write(ob);
                ob.Clear();

                cb.Append("\n}");//Connections end
                FBXwriter.Write(cb);
                cb.Clear();
            }
        }
        private void MeshFBX(DrawableGeometry Mesh, int MeshID, StringBuilder ob)
        {
            if (Mesh.VerticesCount > 0)//general failsafe
            {
                ob.AppendFormat("\n\tGeometry: 3{0}, \"Geometry::\", \"Mesh\" {{", MeshID);
                ob.Append("\n\t\tProperties70:  {");
                ob.AppendFormat("\n\t\t\tP: \"Color\", \"ColorRGB\", \"Color\", \"\",1.0,1.0,1.0");
                ob.Append("\n\t\t}");

                #region Vertices
                ob.AppendFormat("\n\t\tVertices: *{0} {{\n\t\t\ta: ", Mesh.VerticesCount * 3);

                int lineSplit = ob.Length;
                Vector3[] Positions = GetMeshPositions(Mesh);
                for (int v = 0; v < Positions.Length; v++)
                {
                    ob.AppendFormat("{0},{1},{2},", Positions[v].X, Positions[v].Y, Positions[v].Z);

                    if (ob.Length - lineSplit > 2000)
                    {
                        ob.Append("\n");
                        lineSplit = ob.Length;
                    }
                }
                ob.Length--;//remove last comma
                ob.Append("\n\t\t}");
#endregion

#region Indices
                //in order to test topology for triangles/quads we need to store submeshes and write each one as geometry, then link to Mesh Node
                ob.AppendFormat("\n\t\tPolygonVertexIndex: *{0} {{\n\t\t\ta: ", Mesh.IndicesCount);

                lineSplit = ob.Length;
                for (int f = 0; f < Mesh.IndicesCount / 3; f++)
                {
                    ob.AppendFormat("{0},{1},{2},", Mesh.IndexBuffer.Indices[f * 3], Mesh.IndexBuffer.Indices[f * 3 + 2], (-Mesh.IndexBuffer.Indices[f * 3 + 1] - 1));

                    if (ob.Length - lineSplit > 2000)
                    {
                        ob.Append("\n");
                        lineSplit = ob.Length;
                    }
                }
                ob.Length--;//remove last comma

                ob.Append("\n\t\t}");
                ob.Append("\n\t\tGeometryVersion: 124");
#endregion

#region Normals
                Vector3[] Normals = GetMeshNormals(Mesh);
                if (Normals!=null)
                {
                    ob.Append("\n\t\tLayerElementNormal: 0 {");
                    ob.Append("\n\t\t\tVersion: 101");
                    ob.Append("\n\t\t\tName: \"\"");
                    ob.Append("\n\t\t\tMappingInformationType: \"ByVertice\"");
                    ob.Append("\n\t\t\tReferenceInformationType: \"Direct\"");
                    ob.AppendFormat("\n\t\t\tNormals: *{0} {{\n\t\t\ta: ", (Mesh.VerticesCount * 3));

                    lineSplit = ob.Length;
                    for (int v = 0; v < Normals.Length; v++)
                    {
                        ob.AppendFormat("{0},{1},{2},", Normals[v].X, Normals[v].Y, Normals[v].Z);

                        if (ob.Length - lineSplit > 2000)
                        {
                            ob.Append("\n");
                            lineSplit = ob.Length;
                        }
                    }
                    ob.Length--;//remove last comma
                    ob.Append("\n\t\t\t}\n\t\t}");
                }
#endregion

#region Tangents
                Vector3[] Tangents = GetMeshTangents(Mesh);
                if (Tangents!=null)
                {
                    ob.Append("\n\t\tLayerElementTangent: 0 {");
                    ob.Append("\n\t\t\tVersion: 101");
                    ob.Append("\n\t\t\tName: \"\"");
                    ob.Append("\n\t\t\tMappingInformationType: \"ByVertice\"");
                    ob.Append("\n\t\t\tReferenceInformationType: \"Direct\"");
                    ob.AppendFormat("\n\t\t\tTangents: *{0} {{\n\t\t\ta: ", Mesh.VerticesCount * 3);

                    lineSplit = ob.Length;
                    for (int v = 0; v < Tangents.Length; v++)
                    {
                        ob.AppendFormat("{0},{1},{2},", Tangents[v].X, Tangents[v].Y, Tangents[v].Z);

                        if (ob.Length - lineSplit > 2000)
                        {
                            ob.Append("\n");
                            lineSplit = ob.Length;
                        }
                    }
                    ob.Length--;//remove last comma
                    ob.Append("\n\t\t\t}\n\t\t}");
                }
#endregion

#region UV1
                //does FBX support UVW coordinates?
                Vector2[] UVs = GetMeshUVs(Mesh);
                if (UVs != null)
                {
                    ob.Append("\n\t\tLayerElementUV: 0 {");
                    ob.Append("\n\t\t\tVersion: 101");
                    ob.Append("\n\t\t\tName: \"UVChannel_1\"");
                    ob.Append("\n\t\t\tMappingInformationType: \"ByVertice\"");
                    ob.Append("\n\t\t\tReferenceInformationType: \"Direct\"");
                    ob.AppendFormat("\n\t\t\tUV: *{0} {{\n\t\t\ta: ", Mesh.VerticesCount * 2);

                    lineSplit = ob.Length;
                    for (int v = 0; v < UVs.Length; v++)
                    {
                        ob.AppendFormat("{0},{1},", UVs[v].X, 1 - UVs[v].Y);

                        if (ob.Length - lineSplit > 2000)
                        {
                            ob.Append("\n");
                            lineSplit = ob.Length;
                        }
                    }
                    ob.Length--;//remove last comma
                    ob.Append("\n\t\t\t}\n\t\t}");
                }
#endregion

#region Material
                ob.Append("\n\t\tLayerElementMaterial: 0 {");
                ob.Append("\n\t\t\tVersion: 101");
                ob.Append("\n\t\t\tName: \"\"");
                ob.Append("\n\t\t\tMappingInformationType: \"");
                ob.Append("AllSame\"");
                ob.Append("\n\t\t\tReferenceInformationType: \"IndexToDirect\"");
            //  ob.AppendFormat("\n\t\t\tMaterials: *{0} {{", Mesh.TrianglesCount);
                ob.AppendFormat("\n\t\t\tMaterials: *{0} {{", 1);
                ob.Append("\n\t\t\t\t");
            //  ob.Append("0");
                ob.Append("a: 0");
                ob.Append("\n\t\t\t}\n\t\t}");
#endregion

#region Layers
                ob.Append("\n\t\tLayer: 0 {");
                ob.Append("\n\t\t\tVersion: 100");
                if (Normals!=null)
                {
                    ob.Append("\n\t\t\tLayerElement:  {");
                    ob.Append("\n\t\t\t\tType: \"LayerElementNormal\"");
                    ob.Append("\n\t\t\t\tTypedIndex: 0");
                    ob.Append("\n\t\t\t}");
                }
                if (Tangents!=null)
                {
                    ob.Append("\n\t\t\tLayerElement:  {");
                    ob.Append("\n\t\t\t\tType: \"LayerElementTangent\"");
                    ob.Append("\n\t\t\t\tTypedIndex: 0");
                    ob.Append("\n\t\t\t}");
                }
                ob.Append("\n\t\t\tLayerElement:  {");
                ob.Append("\n\t\t\t\tType: \"LayerElementMaterial\"");
                ob.Append("\n\t\t\t\tTypedIndex: 0");
                ob.Append("\n\t\t\t}");
                //
                /*ob.Append("\n\t\t\tLayerElement:  {");
                ob.Append("\n\t\t\t\tType: \"LayerElementTexture\"");
                ob.Append("\n\t\t\t\tTypedIndex: 0");
                ob.Append("\n\t\t\t}");
                ob.Append("\n\t\t\tLayerElement:  {");
                ob.Append("\n\t\t\t\tType: \"LayerElementBumpTextures\"");
                ob.Append("\n\t\t\t\tTypedIndex: 0");
                ob.Append("\n\t\t\t}");*/
                if (UVs!=null)
                {
                    ob.Append("\n\t\t\tLayerElement:  {");
                    ob.Append("\n\t\t\t\tType: \"LayerElementUV\"");
                    ob.Append("\n\t\t\t\tTypedIndex: 0");
                    ob.Append("\n\t\t\t}");
                }
                ob.Append("\n\t\t}"); //Layer 0 end
#endregion

                ob.Append("\n\t}"); //Geometry end
            }
        }
    }
}
