using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VulkanBase.Collada;

namespace VulkanBase.Collada
{
    public class SceneMesh
    {
        public SceneMesh()
        {
            TextureIds = new List<int>();
            TransformationMatrix = Matrix4.Identity;
            Material = new Material();
            Textures = new List<Bitmap>();
        }
        public Matrix4 TransformationMatrix { get; set; }
        public string Id { get; set; }
        public List<int> TextureIds { get; set; }
        public string RootBoneName { get; set; }
        public List<List<Joint>> JointList { get; set; }
        public List<List<float>> WeightList { get; set; }
        public Matrix4 BindShapeMatrix { get; set; }
        public List<Vector3> VertexList { get; set; }
        public List<Vector3> NormalList { get; set; }
        public List<Vector2> TexCoordList { get; set; }
        public List<int> IndexInts { get; set; }

        public Material Material { get; set; }
        public List<Bitmap> Textures { get; internal set; }
    }

    public static class ColladaParser
    {
        private static List<SceneMesh> sceneMeshes;
        private static Dictionary<string, Joint> globalJointDictionary;

        private static XmlNamespaceManager nsmgr;
        private static XmlDocument xmlDocument;
        private static string path;
        private static Dictionary<Joint, Matrix4> translationMatrixForRootBone;

        private static Matrix4 DimensionMatrix = /*/Matrix4.Identity;  /*/
            new Matrix4(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, 0, -1, 0),
                new Vector4(0, 1f, 0, 0),
                new Vector4(0, 0, 0, 1));
        /**/



        public static Model ProcessXml(string filename)
        {
            xmlDocument = new XmlDocument();
            xmlDocument.Load(filename);

            nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
            nsmgr.AddNamespace("x", xmlDocument.DocumentElement.NamespaceURI);

            path = filename.Substring(0, filename.LastIndexOf(@"\") + 1);

            sceneMeshes = new List<SceneMesh>();
            globalJointDictionary = new Dictionary<string, Joint>();
            translationMatrixForRootBone = new Dictionary<Joint, Matrix4>();

            XmlNodeList sceneElements = xmlDocument.SelectNodes(@"/x:COLLADA/x:library_visual_scenes/x:visual_scene/x:node", nsmgr);

            CreateJoints(sceneElements);
            CreateSceneMeshes(sceneElements);

            CreateAnimations();

            if (globalJointDictionary.Values.Count > 0)
            {
                UpdateMatricesAndOrigins();
            }
            return BuildMeshes();
        }

        private static void UpdateMatricesAndOrigins()
        {
            foreach (Joint rootJoint in (from j in globalJointDictionary.Values
                                         where j.ParentJoint == null
                                         select j))
            {
                List<float> theTimestamps = rootJoint.PoseTransformations.Keys.ToList();
                UpdateTransformationMatrices(rootJoint, null);

                foreach (float timestamp in theTimestamps)
                {
                    UpdateTransformationMatrices(rootJoint, timestamp);
                }
                CalculateAnimationLessJointOrigins(rootJoint);

                foreach (float timestamp in theTimestamps)
                {
                    CalculateJointOrigins(rootJoint, timestamp);
                }
            }

            foreach (Joint joint in globalJointDictionary.Values)
            {
                joint.InverseBindQuaternion = joint.InverseBindMatrix.ExtractRotation();
                joint.InverseBindQuaternion = (DimensionMatrix.Inverted() * joint.InverseBindMatrix).ExtractRotation();
                joint.Origin = Vector4.Transform(new Vector4(joint.Origin, 1), DimensionMatrix).Xyz;

                foreach (float timestamp in joint.PoseOrigins.Keys.ToList())
                {
                    joint.PoseOrigins[timestamp] = Vector4.Transform(new Vector4(joint.PoseOrigins[timestamp], 1), DimensionMatrix).Xyz;
                }
            }
        }

        private static void CreateJoints(XmlNodeList sceneElements)
        {
            foreach (XmlNode element in sceneElements)
            {
                XmlNode jointElement = element.SelectSingleNode(@"./x:node[@type='JOINT']", nsmgr);

                if (jointElement != null)
                {
                    ProcessArmatureNode(element, jointElement);
                }
            }
        }

        private static void CreateSceneMeshes(XmlNodeList sceneElements)
        {
            foreach (XmlNode element in sceneElements)
            {
                XmlNode geometryElement = element.SelectSingleNode(@"./x:instance_geometry", nsmgr);
                SceneMesh sceneMesh = new SceneMesh();

                if (geometryElement != null)
                {
                    XmlNode matrixNode = element.SelectSingleNode(@"./x:matrix", nsmgr);
                    if (matrixNode != null)
                    {
                        string matrixString = matrixNode.InnerText;
                        List<float> matrixValues = (from f in matrixString.Split(' ').ToList()
                                                    select float.Parse(f, CultureInfo.InvariantCulture)).ToList();

                        Matrix4 matrix = new Matrix4();

                        matrix.M11 = matrixValues[0];
                        matrix.M21 = matrixValues[1];
                        matrix.M31 = matrixValues[2];
                        matrix.M41 = matrixValues[3];
                        matrix.M12 = matrixValues[4];
                        matrix.M22 = matrixValues[5];
                        matrix.M32 = matrixValues[6];
                        matrix.M42 = matrixValues[7];
                        matrix.M13 = matrixValues[8];
                        matrix.M23 = matrixValues[9];
                        matrix.M33 = matrixValues[10];
                        matrix.M43 = matrixValues[11];
                        matrix.M14 = matrixValues[12];
                        matrix.M24 = matrixValues[13];
                        matrix.M34 = matrixValues[14];
                        matrix.M44 = matrixValues[15];

                        sceneMesh.TransformationMatrix = matrix;
                    }
                    element.SelectSingleNode(@"./x:instance_geometry", nsmgr);
                    sceneMesh.Id = element.Attributes["id"].Value;
                    ProcessSceneNodeWithGeometry(geometryElement, sceneMesh);
                    sceneMeshes.Add(sceneMesh);
                }
                else
                {
                    XmlNode controllerElement = element.SelectSingleNode(@"./x:instance_controller", nsmgr);
                    if (controllerElement != null)
                    {
                        sceneMesh.Id = element.Attributes["id"].Value;
                        ProcessSceneNodeWithController(controllerElement, sceneMesh);
                        sceneMeshes.Add(sceneMesh);
                    }
                }
            }
        }

        private static void CreateAnimations()
        {
            XmlNodeList animationNodes = xmlDocument.SelectNodes(@"/x:COLLADA/x:library_animations/x:animation", nsmgr);

            foreach (XmlNode animationNode in animationNodes)
            {
                XmlNode channelNode = animationNode.SelectSingleNode(@"x:channel", nsmgr);
                string animationTarget = channelNode.Attributes["target"].Value;

                string animationType = animationTarget.Substring(animationTarget.IndexOf('/') + 1);

                if (animationType == "transform")
                {
                    string targetBone = animationTarget.Substring(0, animationTarget.IndexOf('/'));
                    Joint joint = globalJointDictionary[targetBone];

                    string samplerId = channelNode.Attributes["source"].Value.Substring(1);

                    string inputSource = animationNode.SelectSingleNode(@"x:sampler[@id='" + samplerId + "']/x:input[@semantic='INPUT']", nsmgr).Attributes["source"].Value.Substring(1);

                    string timestampString = animationNode.SelectSingleNode(@"x:source[@id='" + inputSource + "']/x:float_array", nsmgr).InnerText;

                    List<float> timestamps = (from t in timestampString.Split(' ')
                                              select float.Parse(t, CultureInfo.InvariantCulture)).ToList();

                    string outputSource = animationNode.SelectSingleNode(@"x:sampler[@id='" + samplerId + "']/x:input[@semantic='OUTPUT']", nsmgr).Attributes["source"].Value.Substring(1);

                    string matrixString = animationNode.SelectSingleNode(@"x:source[@id='" + outputSource + "']/x:float_array", nsmgr).InnerText;

                    List<float> matrixValues = (from f in matrixString.Split(' ').ToList()
                                                select float.Parse(f, CultureInfo.InvariantCulture)).ToList();

                    List<Matrix4> matrices = new List<Matrix4>();
                    List<Quaternion> quaternions = new List<Quaternion>();

                    for (int i = 0; i < matrixValues.Count; i += 16)
                    {
                        Matrix4 matrix = new Matrix4();

                        matrix.M11 = matrixValues[i];
                        matrix.M21 = matrixValues[i + 1];
                        matrix.M31 = matrixValues[i + 2];
                        matrix.M41 = matrixValues[i + 3];
                        matrix.M12 = matrixValues[i + 4];
                        matrix.M22 = matrixValues[i + 5];
                        matrix.M32 = matrixValues[i + 6];
                        matrix.M42 = matrixValues[i + 7];
                        matrix.M13 = matrixValues[i + 8];
                        matrix.M23 = matrixValues[i + 9];
                        matrix.M33 = matrixValues[i + 10];
                        matrix.M43 = matrixValues[i + 11];
                        matrix.M14 = matrixValues[i + 12];
                        matrix.M24 = matrixValues[i + 13];
                        matrix.M34 = matrixValues[i + 14];
                        matrix.M44 = matrixValues[i + 15];

                        matrices.Add(matrix);

                        Quaternion quaternion = matrix.ExtractRotation();

                        if (joint.ParentJoint == null)
                        {
                            quaternion = (Matrix4.CreateRotationX((float)Math.PI / -2f) * matrix).ExtractRotation();
                        }
                        //Skeleton[i].InterpolatedQuaternion = Matrix4.CreateRotationX((float)Math.PI / -2f).ExtractRotation() * Quaternion.Slerp(Skeleton[i].PoseQuaternions[timeV1], Skeleton[i].PoseQuaternions[timeV2], blend);

                        quaternions.Add(quaternion);
                    }

                    for (int i = 0; i < timestamps.Count; i++)
                    {
                        joint.PoseQuaternions.Add(timestamps[i], quaternions[i]);
                        joint.PoseTransformations.Add(timestamps[i], matrices[i]);
                    }
                }
            }

            if (globalJointDictionary.Values.Count > 0)
            {
                Joint rootJoint = (from j in globalJointDictionary.Values
                                   where j.ParentJoint == null
                                   select j).First();

                Matrix4 translationMatrix = translationMatrixForRootBone[rootJoint];

                foreach (Joint joint in globalJointDictionary.Values)
                {
                    foreach (float timestamp in joint.PoseQuaternions.Keys)
                    {
                        joint.PoseOrigins.Add(timestamp, new Vector3());
                    }
                }
            }
        }


        private static Dictionary<string, Matrix4> globalTestArray = new Dictionary<string, Matrix4>();


        private static List<Joint> GetParentJoints(Joint joint)
        {
            List<Joint> theList;
            if (joint.ParentJoint != null)
            {
                theList = GetParentJoints(joint.ParentJoint);
                theList.Add(joint);
            }
            else
            {
                theList = new List<Joint>() { joint };
            }
            return theList;
        }

        private static void CalculateJointOrigins(Joint joint, float timestamp)
        {
            foreach (Joint childJoint in (from j in globalJointDictionary.Values where j.ParentJoint == joint select j).ToList())
            {
                CalculateJointOrigins(childJoint, timestamp);
            }

            joint.PoseOrigins[timestamp] = Vector4.Transform(new Vector4(0, 0, 0, 1), joint.PoseTransformations[timestamp]).Xyz;
        }

        private static void CalculateAnimationLessJointOrigins(Joint joint)
        {
            foreach (Joint childJoint in (from j in globalJointDictionary.Values where j.ParentJoint == joint select j).ToList())
            {
                CalculateAnimationLessJointOrigins(childJoint);
            }

            joint.Origin = Vector4.Transform(new Vector4(0, 0, 0, 1), joint.TransformationMatrix).Xyz;
        }

        private static void UpdateTransformationMatrices(Joint joint, float? timestamp)
        {
            foreach (Joint childJoint in (from j in globalJointDictionary.Values where j.ParentJoint == joint select j).ToList())
            {
                UpdateTransformationMatrices(childJoint, timestamp);
            }
            List<Joint> jointList = GetParentJoints(joint);

            Matrix4 matrix = joint.TransformationMatrix;
            Quaternion quaternion = joint.TheQuaternion;
            if (timestamp.HasValue)
            {
                matrix = joint.PoseTransformations[timestamp.Value];
                quaternion = joint.PoseQuaternions[timestamp.Value];
            }

            for (int i = jointList.Count() - 2; i >= 0; i--)
            {
                if (timestamp.HasValue)
                {
                    matrix = matrix * jointList[i].PoseTransformations[timestamp.Value];
                    quaternion = jointList[i].PoseQuaternions[timestamp.Value] * quaternion;
                }
                else
                {
                    matrix = matrix * jointList[i].TransformationMatrix;
                    quaternion = jointList[i].TheQuaternion * quaternion;
                }

            }

            if (timestamp.HasValue)
            {
                joint.PoseTransformations[timestamp.Value] = matrix;
                joint.PoseQuaternions[timestamp.Value] = quaternion;
            }
            else
            {
                joint.TransformationMatrix = matrix;
                joint.TheQuaternion = quaternion;
            }
        }


        private static Matrix4 CreateFromQuaternion(Quaternion q)
        {
            Matrix4 result = Matrix4.Identity;

            float X = q.X;
            float Y = q.Y;
            float Z = q.Z;
            float W = q.W;

            float xx = X * X;
            float xy = X * Y;
            float xz = X * Z;
            float xw = X * W;
            float yy = Y * Y;
            float yz = Y * Z;
            float yw = Y * W;
            float zz = Z * Z;
            float zw = Z * W;

            result.M11 = 1 - 2 * (yy + zz);
            result.M21 = 2 * (xy - zw);
            result.M31 = 2 * (xz + yw);
            result.M12 = 2 * (xy + zw);
            result.M22 = 1 - 2 * (xx + zz);
            result.M32 = 2 * (yz - xw);
            result.M13 = 2 * (xz - yw);
            result.M23 = 2 * (yz + xw);
            result.M33 = 1 - 2 * (xx + yy);
            return result;
        }

        private static void ProcessArmatureNode(XmlNode element, XmlNode jointElement)
        {
            string id = element.Attributes["id"].Value;
            XmlNode armatureTranslationNode = element.SelectSingleNode(@"./x:translate[@sid='location']", nsmgr);

            Matrix4 TranslationMatrix = Matrix4.Identity;
            if (armatureTranslationNode != null)
            {
                string translationString = element.SelectSingleNode(@"./x:translate[@sid='location']", nsmgr).InnerText;
                List<float> tranlationCoordinates = (from f in translationString.Split(' ').ToList()
                                                     select float.Parse(f, CultureInfo.InvariantCulture)).ToList();
                TranslationMatrix = Matrix4.CreateTranslation(tranlationCoordinates[0], tranlationCoordinates[1], tranlationCoordinates[2]);
            }

            List<Joint> jointList = new List<Joint>();
            BuildJoint(jointElement, jointList);

            translationMatrixForRootBone.Add(jointList.Last(), TranslationMatrix);
        }

        private static void BuildJoint(XmlNode jointNode, List<Joint> jointList)
        {
            BuildJoint(jointNode, null, jointList);
        }

        private static void BuildJoint(XmlNode jointNode, Joint parentJoint, List<Joint> jointList)
        {
            BuildJoint(jointNode, parentJoint, true, jointList);
        }

        private static void BuildJoint(XmlNode jointNode, Joint parentJoint, bool buildJoint, List<Joint> jointList)
        {
            Joint joint;
            if (buildJoint)
            {
                joint = new Joint()
                {
                    Sid = jointNode.Attributes["sid"].Value,
                    ParentJoint = parentJoint
                };
            }
            else
            {
                joint = parentJoint;
            }

            for (XmlNode childJointNodes = jointNode.FirstChild; childJointNodes != null; childJointNodes = childJointNodes.NextSibling)
            {
                if (childJointNodes.Name == "matrix")
                {
                    if (buildJoint)
                    {
                        Matrix4 transformationMatrix = BuildMatrixFromString(childJointNodes.InnerText);
                        joint.TransformationMatrix = transformationMatrix;
                        joint.TheQuaternion = transformationMatrix.ExtractRotation();
                    }
                }
                else if (childJointNodes.Attributes["type"] != null &&
                         (childJointNodes.Attributes["type"].Value == "JOINT" ||
                          childJointNodes.Attributes["type"].Value == "NODE"))
                {
                    BuildJoint(
                        childJointNodes,
                        joint,
                        childJointNodes.Attributes["type"].Value == "JOINT",
                        jointList);
                }
            }
            if (buildJoint)
            {
                globalJointDictionary.Add(joint.Sid, joint);
                jointList.Add(joint);
            }

        }

        private static Matrix4 BuildMatrixFromString(string matrixString)
        {
            Matrix4 matrix = new Matrix4();
            List<float> matrixValues =
                    (from f in matrixString.Split(' ').ToList()
                     select float.Parse(f, CultureInfo.InvariantCulture)).ToList();

            matrix.M11 = matrixValues[0];
            matrix.M21 = matrixValues[1];
            matrix.M31 = matrixValues[2];
            matrix.M41 = matrixValues[3];
            matrix.M12 = matrixValues[4];
            matrix.M22 = matrixValues[5];
            matrix.M32 = matrixValues[6];
            matrix.M42 = matrixValues[7];
            matrix.M13 = matrixValues[8];
            matrix.M23 = matrixValues[9];
            matrix.M33 = matrixValues[10];
            matrix.M43 = matrixValues[11];
            matrix.M14 = matrixValues[12];
            matrix.M24 = matrixValues[13];
            matrix.M34 = matrixValues[14];
            matrix.M44 = matrixValues[15];

            return matrix;
        }

        private static void ProcessSceneNodeWithGeometry(XmlNode geometryElement, SceneMesh sceneMesh)
        {
            string geometryUrl = geometryElement.Attributes["url"].Value.Substring(1);

            AddVectorArrays(sceneMesh, geometryUrl);

            XmlNode materialNode = geometryElement.SelectSingleNode(@"x:bind_material/x:technique_common/x:instance_material", nsmgr);

            if (materialNode != null)
            {
                string materialUrl = materialNode.Attributes["target"].Value.Substring(1);

                AddTextureFromMaterial(materialUrl, sceneMesh); 
            }
        }

        private static void AddTextureFromMaterial(string materialUrl, SceneMesh sceneMesh)
        {
            string effectUrl = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_materials/x:material[@id='" + materialUrl + "']/x:instance_effect", nsmgr).Attributes["url"].Value.Substring(1); ;



            XmlNode textureNode = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:technique/x:phong/x:diffuse/x:texture", nsmgr);
            string textureSamplerName = string.Empty;
            string bumpMapTextureSamplerName = string.Empty;
            if (textureNode == null)
            {
                textureNode = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:technique/x:lambert/x:diffuse/x:texture", nsmgr);
                if (textureNode != null)
                {
                    textureSamplerName = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:technique/x:lambert/x:diffuse/x:texture", nsmgr).Attributes["texture"].Value;
                    XmlNode bumpMapNode = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:technique/x:extra/x:technique/x:bump/x:texture", nsmgr);
                    if (bumpMapNode != null)
                    {
                        bumpMapTextureSamplerName = bumpMapNode.Attributes["texture"].Value;
                    }

                }
            }
            else
            {
                textureSamplerName = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:technique/x:phong/x:diffuse/x:texture", nsmgr).Attributes["texture"].Value;

                XmlNode specularityNode = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:technique/x:phong/x:specular/x:color", nsmgr);
                if (specularityNode != null)
                {
                    float[] specularityColorValues = (from f in specularityNode.InnerText.Split(' ') select Single.Parse(f)).ToArray();
                    Vector3 specularColor = new Vector3(specularityColorValues[0], specularityColorValues[1], specularityColorValues[2]);
                    int shininess = Int32.Parse(xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:technique/x:phong/x:shininess/x:float", nsmgr).InnerText);
                    sceneMesh.Material = new Material(specularColor, shininess);
                }
            }

            if (textureNode != null)
            {

                string samplerSource = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:newparam[@sid='" + textureSamplerName + "']/x:sampler2D/x:source", nsmgr).InnerText;

                string textureSource = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:newparam[@sid='" + samplerSource + "']/x:surface/x:init_from", nsmgr).InnerText;

                string imageName = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_images/x:image[@id='" + textureSource + "']/x:init_from", nsmgr).InnerText;

                sceneMesh.Textures.Add(TextureLibrary.ObtainImage(path + imageName));

                if (!string.IsNullOrEmpty(bumpMapTextureSamplerName))
                {
                    samplerSource = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:newparam[@sid='" + bumpMapTextureSamplerName + "']/x:sampler2D/x:source", nsmgr).InnerText;

                    textureSource = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_effects/x:effect[@id='" + effectUrl + "']/x:profile_COMMON/x:newparam[@sid='" + samplerSource + "']/x:surface/x:init_from", nsmgr).InnerText;

                    imageName = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_images/x:image[@id='" + textureSource + "']/x:init_from", nsmgr).InnerText;

                    sceneMesh.Textures.Add(TextureLibrary.ObtainImage(path + imageName));
                    sceneMesh.Material.HasNormalMapping = true;
                }
            }
        }

        private static int AddVectorArrays(SceneMesh sceneMesh, string geometryUrl)
        {
            XmlNode geometryMeshNode = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_geometries/x:geometry[@id='" + geometryUrl + "']/x:mesh", nsmgr);

            XmlNode vertexInput = geometryMeshNode.SelectSingleNode(@"x:polylist/x:input[@semantic='VERTEX']", nsmgr);

            string positionSourceUrl = geometryMeshNode.SelectSingleNode(@"x:vertices[@id='" + vertexInput.Attributes["source"].Value.Substring(1) + "']/x:input[@semantic='POSITION']", nsmgr).Attributes["source"].Value.Substring(1);

            string vertexPositionArrayString = geometryMeshNode.SelectSingleNode(@"x:source[@id='" + positionSourceUrl + "']/x:float_array", nsmgr).InnerText;

            List<float> vertexFloats = (from f in vertexPositionArrayString.Split(' ').ToList()
                                        select float.Parse(f, CultureInfo.InvariantCulture)).ToList();

            List<Vector3> vertexList = new List<Vector3>();
            for (int i = 0; i < vertexFloats.Count; i += 3)
            {
                vertexList.Add(
                    Vector4.Transform(
                        new Vector4(
                            vertexFloats[i],
                            vertexFloats[i + 1],
                            vertexFloats[i + 2],
                            1f),
                        sceneMesh.TransformationMatrix).Xyz
                    );
            }

            XmlNode normalInput = geometryMeshNode.SelectSingleNode(@"x:polylist/x:input[@semantic='NORMAL']", nsmgr);

            string normalArrayString = geometryMeshNode.SelectSingleNode(@"x:source[@id='" + normalInput.Attributes["source"].Value.Substring(1) + "']/x:float_array", nsmgr).InnerText;

            List<float> normalFloats = (from f in normalArrayString.Split(' ').ToList()
                                        select float.Parse(f, CultureInfo.InvariantCulture)).ToList();

            List<Vector3> normalList = new List<Vector3>();
            for (int i = 0; i < normalFloats.Count; i += 3)
            {
                normalList.Add(
                    Vector4.Transform(
                        new Vector4(
                            normalFloats[i],
                            normalFloats[i + 1],
                            normalFloats[i + 2],
                            0f),
                        sceneMesh.TransformationMatrix).Xyz);
            }

            XmlNode texCoordInput = geometryMeshNode.SelectSingleNode(@"x:polylist/x:input[@semantic='TEXCOORD']", nsmgr);
            List<Vector2> texCoordList = new List<Vector2>();
            if (texCoordInput != null)
            {
                string texCoordArrayString = geometryMeshNode.SelectSingleNode(@"x:source[@id='" + texCoordInput.Attributes["source"].Value.Substring(1) + "']/x:float_array", nsmgr).InnerText;

                List<float> texCoordFloats = (from f in texCoordArrayString.Split(' ').ToList()
                                              select float.Parse(f, CultureInfo.InvariantCulture)).ToList();

                for (int i = 0; i < texCoordFloats.Count; i += 2)
                {
                    texCoordList.Add(
                        new Vector2(
                            texCoordFloats[i],
                            1 - texCoordFloats[i + 1]));
                }
            }

            string indicesArrayString = geometryMeshNode.SelectSingleNode(@"x:polylist/x:p", nsmgr).InnerText;
            sceneMesh.IndexInts = (from i in indicesArrayString.Split(' ').ToList()
                                   select int.Parse(i, CultureInfo.InvariantCulture)).ToList();

            int stride = (from node in new List<XmlNode>(geometryMeshNode.SelectNodes(@"x:polylist/x:input", nsmgr).Cast<XmlNode>())
                          select int.Parse(node.Attributes["offset"].Value)).Max() + 1;

            sceneMesh.VertexList = new List<Vector3>();
            sceneMesh.NormalList = new List<Vector3>();
            sceneMesh.TexCoordList = new List<Vector2>();

            for (int i = 0; i < sceneMesh.IndexInts.Count; i += stride)
            {
                sceneMesh.VertexList.Add(Vector4.Transform(new Vector4(vertexList[sceneMesh.IndexInts[i + int.Parse(vertexInput.Attributes["offset"].Value, CultureInfo.InvariantCulture)]], 1), DimensionMatrix).Xyz);
                //sceneMesh.VertexList.Add(Vector4.Transform(new Vector4(vertexList[sceneMesh.IndexInts[i + int.Parse(vertexInput.Attributes["offset"].Value, CultureInfo.InvariantCulture)]], 1), Matrix4.Identity).Xyz);

                sceneMesh.NormalList.Add(Vector4.Transform(new Vector4(normalList[sceneMesh.IndexInts[i + int.Parse(normalInput.Attributes["offset"].Value, CultureInfo.InvariantCulture)]], 0), DimensionMatrix).Xyz);
                if (texCoordInput != null)
                {
                    sceneMesh.TexCoordList.Add(texCoordList[sceneMesh.IndexInts[i + int.Parse(texCoordInput.Attributes["offset"].Value, CultureInfo.InvariantCulture)]]);
                }
            }

            return stride;
        }


        private static void ProcessSceneNodeWithController(XmlNode controllerElement, SceneMesh sceneMesh)
        {
            string skinUrl = controllerElement.Attributes["url"].Value.Substring(1);

            XmlNode skinNode = xmlDocument.SelectSingleNode(@"/x:COLLADA/x:library_controllers/x:controller[@id='" + skinUrl + "']", nsmgr);

            string geometryUrl = skinNode.SelectSingleNode(@"x:skin", nsmgr).Attributes["source"].Value.Substring(1);

            XmlNode instanceMaterialNode = controllerElement.SelectSingleNode(@"x:bind_material/x:technique_common/x:instance_material", nsmgr);
            if (instanceMaterialNode != null)
            {
                string materialUrl = instanceMaterialNode.Attributes["target"].Value.Substring(1);

                AddTextureFromMaterial(materialUrl, sceneMesh);
            }


            sceneMesh.RootBoneName = controllerElement.SelectSingleNode(@"x:skeleton", nsmgr).InnerText.Substring(1);

            string bindShapeMatrixString = skinNode.SelectSingleNode(@"x:skin/x:bind_shape_matrix", nsmgr).InnerText;

            sceneMesh.BindShapeMatrix = BuildMatrixFromString(bindShapeMatrixString);
            sceneMesh.BindShapeMatrix = SwitchYandZ(sceneMesh.BindShapeMatrix);

            sceneMesh.JointList = new List<List<Joint>>();
            sceneMesh.WeightList = new List<List<float>>();

            XmlNode jointInput = skinNode.SelectSingleNode(@"x:skin/x:vertex_weights/x:input[@semantic='JOINT']", nsmgr);

            string jointArrayString = skinNode.SelectSingleNode(@"x:skin/x:source[@id='" + jointInput.Attributes["source"].Value.Substring(1) + "']/x:Name_array", nsmgr).InnerText;

            List<Joint> jointsOfMesh = (from js in jointArrayString.Split(' ')
                                        select globalJointDictionary[js]).ToList();


            string invBindMatrixString = skinNode.SelectSingleNode(@"x:skin/x:source[@id='" + skinNode.SelectSingleNode(@"x:skin/x:joints/x:input[@semantic='INV_BIND_MATRIX']", nsmgr).Attributes["source"].Value.Substring(1) + "']/x:float_array", nsmgr).InnerText;

            List<float> matrixValues = (from f in invBindMatrixString.Split(' ').ToList()
                                        select float.Parse(f, CultureInfo.InvariantCulture)).ToList();

            List<Quaternion> quaternions = new List<Quaternion>();

            for (int i = 0; i < matrixValues.Count && i / 16 < jointsOfMesh.Count; i += 16)
            {
                Matrix4 matrix = new Matrix4();

                matrix.M11 = matrixValues[i];
                matrix.M21 = matrixValues[i + 1];
                matrix.M31 = matrixValues[i + 2];
                matrix.M41 = matrixValues[i + 3];
                matrix.M12 = matrixValues[i + 4];
                matrix.M22 = matrixValues[i + 5];
                matrix.M32 = matrixValues[i + 6];
                matrix.M42 = matrixValues[i + 7];
                matrix.M13 = matrixValues[i + 8];
                matrix.M23 = matrixValues[i + 9];
                matrix.M33 = matrixValues[i + 10];
                matrix.M43 = matrixValues[i + 11];
                matrix.M14 = matrixValues[i + 12];
                matrix.M24 = matrixValues[i + 13];
                matrix.M34 = matrixValues[i + 14];
                matrix.M44 = matrixValues[i + 15];

                jointsOfMesh[i / 16].InverseBindMatrix = matrix;

                jointsOfMesh[i / 16].InverseBindQuaternion = matrix.ExtractRotation();
            }
            XmlNode weightInput = skinNode.SelectSingleNode(@"x:skin/x:vertex_weights/x:input[@semantic='WEIGHT']", nsmgr);

            string weightArrayString = skinNode.SelectSingleNode(@"x:skin/x:source[@id='" + weightInput.Attributes["source"].Value.Substring(1) + "']/x:float_array", nsmgr).InnerText;

            List<float> weightsOfMesh = (from w in weightArrayString.Split(' ')
                                         select float.Parse(w, CultureInfo.InvariantCulture)).ToList();

            int stride = 2;

            string vcountString = skinNode.SelectSingleNode(@"x:skin/x:vertex_weights/x:vcount", nsmgr).InnerText.Trim();
            List<int> vCount = (from vs in vcountString.Split(' ')
                                select int.Parse(vs, CultureInfo.InvariantCulture)).ToList();

            string vString = skinNode.SelectSingleNode(@"x:skin/x:vertex_weights/x:v", nsmgr).InnerText.Trim();
            List<int> v = (from vs in vString.Split(' ')
                           select int.Parse(vs, CultureInfo.InvariantCulture)).ToList();

            int index = 0;

            List<List<Joint>> jointList = new List<List<Joint>>();
            List<List<float>> weightList = new List<List<float>>();
            for (int i = 0; i < vCount.Count; i++)
            {
                jointList.Add(new List<Joint>());
                weightList.Add(new List<float>());
                for (int j = 0; j < vCount[i]; j++)
                {
                    int jointIndex = v[index + int.Parse(jointInput.Attributes["offset"].Value)];
                    jointList[i].Add(jointsOfMesh[jointIndex]);

                    int weighIndex = v[index + int.Parse(weightInput.Attributes["offset"].Value)];
                    weightList[i].Add(weightsOfMesh[weighIndex]);

                    index += stride;
                }
            }

            stride = AddVectorArrays(sceneMesh, geometryUrl);

            for (int i = 0; i < sceneMesh.IndexInts.Count; i += stride)
            {
                sceneMesh.JointList.Add(jointList[sceneMesh.IndexInts[i]]);
                sceneMesh.WeightList.Add(weightList[sceneMesh.IndexInts[i]]);
            }
        }

        private static Matrix4 SwitchYandZ(Matrix4 matrix4)
        {
            Matrix4 bindShapeMatrix = matrix4;
            Matrix4 bindShapeMatrixTransposed;
            Matrix4.Transpose(ref bindShapeMatrix, out bindShapeMatrixTransposed);
            bindShapeMatrix = bindShapeMatrixTransposed;
            bindShapeMatrix = new Matrix4(bindShapeMatrix.Row0, bindShapeMatrix.Row2, bindShapeMatrix.Row1, bindShapeMatrix.Row3);
            Matrix4.Transpose(ref bindShapeMatrix, out bindShapeMatrixTransposed);
            return bindShapeMatrixTransposed;
        }


        private static void WriteJointAndChildrenInList(Joint joint, List<Joint> jointList)
        {
            jointList.Add(joint);

            foreach (Joint childJoint in (from j in globalJointDictionary.Values where j.ParentJoint == joint select j))
            {
                WriteJointAndChildrenInList(childJoint, jointList);
            }
        }

        private static Model BuildMeshes()
        {
            Model model = new Model();

            foreach (var sceneMesh in sceneMeshes)
            {
                Mesh mesh = new Mesh();
                BuildStaticMesh(sceneMesh, mesh);

                if (!string.IsNullOrEmpty(sceneMesh.RootBoneName))
                {
                    BuildAnimatedMesh(sceneMesh, ref mesh);
                }

                
                mesh.TextureManager.Textures = new System.Collections.ObjectModel.ObservableCollection<Texture>(mesh.TextureManager.Textures);

                model.meshes.Add(mesh);

            }
            return model;
        }

        private static void BuildStaticMesh(SceneMesh sceneMesh, Mesh mesh)
        {
            mesh.Name = sceneMesh.Id;
            mesh.Material = sceneMesh.Material;

            mesh.VertexBuffer.Initialize();
            mesh.VertexBuffer.Data = sceneMesh.VertexList.ToArray();

            mesh.NormalBuffer.Initialize();
            mesh.NormalBuffer.Data = sceneMesh.NormalList.ToArray();

            mesh.TextureCoordinateBuffer.Initialize();
            mesh.TextureCoordinateBuffer.Data = sceneMesh.TexCoordList.ToArray();


            mesh.Bitmaps.AddRange(sceneMesh.Textures);

            List<Vector3> tangentBuffer = new List<Vector3>();
            List<Vector3> bitangentBuffer = new List<Vector3>();

            mesh.TangentBuffer.Data = new Vector3[0];
            mesh.BitangentBuffer.Data = new Vector3[0];

            if (mesh.TextureCoordinateBuffer.Data.Count() > 0)
            {
                for (int i = 0; i < mesh.VertexBuffer.Data.Length; i++)
                {
                    if (i > 0 && (i + 1) % 3 == 0)
                    {
                        Vector3 v0 = mesh.VertexBuffer.Data[i - 2];
                        Vector3 v1 = mesh.VertexBuffer.Data[i - 1];
                        Vector3 v2 = mesh.VertexBuffer.Data[i - 0];

                        Vector2 uv0 = mesh.TextureCoordinateBuffer.Data[i - 2];
                        Vector2 uv1 = mesh.TextureCoordinateBuffer.Data[i - 1];
                        Vector2 uv2 = mesh.TextureCoordinateBuffer.Data[i - 0];

                        Vector3 deltaPos1 = v1 - v0;
                        Vector3 deltaPos2 = v2 - v0;

                        Vector2 deltaUV1 = uv1 - uv0;
                        Vector2 deltaUV2 = uv2 - uv0;

                        float r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);

                        Vector3 tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
                        Vector3 bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;

                        for (int j = 0; j < 3; j++)
                        {
                            tangentBuffer.Add(tangent);
                            bitangentBuffer.Add(bitangent);
                        }
                    }
                }

                mesh.TangentBuffer.Data = tangentBuffer.ToArray();
                mesh.BitangentBuffer.Data = bitangentBuffer.ToArray();
            }

            mesh.BindShapeMatrix = sceneMesh.BindShapeMatrix;

        }

        private static void BuildAnimatedMesh(SceneMesh sceneMesh, ref Mesh mesh)
        {
            AnimatedMesh animatedMesh = new AnimatedMesh(mesh);
            Dictionary<Joint, int> jointIndexDict = new Dictionary<Joint, int>();
            WriteJointAndChildrenInList(globalJointDictionary[sceneMesh.RootBoneName], animatedMesh.Skeleton);

            for (int i = 0; i < animatedMesh.Skeleton.Count; i++)
            {
                jointIndexDict.Add(animatedMesh.Skeleton[i], i);
            }


            if (sceneMesh.JointList != null)
            {
                for (int i = 0; i < sceneMesh.JointList.Count; i++)
                {
                    for (int j = 0; j < sceneMesh.JointList[i].Count; j++)
                    {
                        if (!animatedMesh.JointDict.ContainsKey(j))
                        {
                            animatedMesh.JointDict.Add(j, new Joint[sceneMesh.JointList.Count]);
                        }

                        if (!animatedMesh.WeighDict.ContainsKey(j))
                        {
                            animatedMesh.WeighDict.Add(j, new float[sceneMesh.VertexList.Count]);
                        }

                        animatedMesh.JointDict[j][i] = sceneMesh.JointList[i][j];

                        animatedMesh.WeighDict[j][i] = sceneMesh.WeightList[i][j];
                    }

                    Dictionary<Joint, Vector3> updatedOrigin = new Dictionary<Joint, Vector3>();
                    foreach (var joint in globalJointDictionary.Values)
                    {
                        updatedOrigin.Add(joint, new Vector3());
                    }
                    for (int j = 0; j < sceneMesh.JointList[i].Count; j++)
                    {
                        foreach (float timestamp in sceneMesh.JointList[i][j].PoseQuaternions.Keys)
                        {

                        }
                    }
                }

                Joint rootJoint = (from j in globalJointDictionary.Values
                                   where j.ParentJoint == null
                                   select j).First();

                List<float> theTimestamps = rootJoint.PoseTransformations.Keys.ToList();

                List<Vector4> bindPoseOrigins = new List<Vector4>();
                for (int i = 0; i < animatedMesh.Skeleton.Count(); i++)
                {
                    bindPoseOrigins.Add(new Vector4(animatedMesh.Skeleton[i].Origin, 1));
                }
                
                animatedMesh.BindposeOriginBuffer.Data = bindPoseOrigins.ToArray();
                animatedMesh.BindposeOriginBuffer.Initialize();

                animatedMesh.WeightBuffer1.Data = new Vector4[animatedMesh.VertexBuffer.Data.Length];
                animatedMesh.WeightBuffer2.Data = new Vector4[animatedMesh.VertexBuffer.Data.Length];
                animatedMesh.JointBuffer1.Data = new Vector4[animatedMesh.VertexBuffer.Data.Length];
                animatedMesh.JointBuffer2.Data = new Vector4[animatedMesh.VertexBuffer.Data.Length];
                for (int i = 0; i < animatedMesh.VertexBuffer.Data.Length; i++)
                {
                    float[] weightArray = new float[16];
                    float[] indexArray = new float[16];

                    if (animatedMesh.WeighDict[0][i] != 0)
                    {
                        for (int j = 0; j < animatedMesh.WeighDict.Count(); j++)
                        {
                            if (animatedMesh.WeighDict[j][i] == 0)
                            {
                                break;
                            }
                            weightArray[j] = animatedMesh.WeighDict[j][i];
                            indexArray[j] = jointIndexDict[animatedMesh.JointDict[j][i]];


                        }
                    }

                    animatedMesh.WeightBuffer1.Data[i] = new Vector4(
                        weightArray[0], weightArray[1], weightArray[2], weightArray[3]);
                    animatedMesh.WeightBuffer2.Data[i] = new Vector4(
                        weightArray[4], weightArray[5], weightArray[6], weightArray[7]);

                    animatedMesh.JointBuffer1.Data[i] = new Vector4(
                        indexArray[0], indexArray[1], indexArray[2], indexArray[3]);
                    animatedMesh.JointBuffer2.Data[i] = new Vector4(
                        indexArray[4], indexArray[5], indexArray[6], indexArray[7]);


                    /*
                    mesh.Weights[i] = new Matrix4(
                        weightArray[0], weightArray[1], weightArray[2], weightArray[3],
                        weightArray[4], weightArray[5], weightArray[6], weightArray[7],
                        weightArray[8], weightArray[9], weightArray[10], weightArray[11],
                        weightArray[12], weightArray[13], weightArray[14], weightArray[15]);

                    mesh.Indices[i] = new Matrix4(
                        indexArray[0], indexArray[1], indexArray[2], indexArray[3],
                        indexArray[4], indexArray[5], indexArray[6], indexArray[7],
                        indexArray[8], indexArray[9], indexArray[10], indexArray[11],
                        indexArray[12], indexArray[13], indexArray[14], indexArray[15]);
                        */
                }

                /*
                GL.BindBuffer(BufferTarget.ArrayBuffer, mesh.VertexBufferId);
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(mesh.VertexBuffer.Length * Vector3.SizeInBytes), mesh.VertexDictionary[mesh.VertexDictionary.Keys.ToList()[1] ], BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                */
                animatedMesh.WeightBuffer1.Initialize();
                animatedMesh.WeightBuffer2.Initialize();

                animatedMesh.JointBuffer1.Initialize();
                animatedMesh.JointBuffer2.Initialize();


                //animatedMesh.AddBufferTextures();

                animatedMesh.InstanceAnimation();

                mesh = animatedMesh;
            }
        }
    }
}


