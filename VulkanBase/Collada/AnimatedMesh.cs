using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.Collada
{
    public unsafe class AnimatedMesh : Mesh
    {
        public Buffer<Vector4> WeightBuffer1 { get; set; } = new Buffer<Vector4>();
        public Buffer<Vector4> JointBuffer1 { get; set; } = new Buffer<Vector4>();

        public Buffer<Vector4> WeightBuffer2 { get; set; } = new Buffer<Vector4>();
        public Buffer<Vector4> JointBuffer2 { get; set; } = new Buffer<Vector4>();

        public Buffer<Vector4> InterpolatedQuaternionBuffer { get; set; } = new Buffer<Vector4>();
        public Buffer<Vector4> InterpolatedOriginBuffer { get; set; } = new Buffer<Vector4>();
        public Buffer<Vector4> BindposeOriginBuffer { get; set; } = new Buffer<Vector4>();



        public List<Joint> Skeleton = new List<Joint>();

        private Joint _rootJoint;
        private float[] _timestamps;

        private Joint RootJoint
        {
            get
            {
                if (_rootJoint == null)
                {
                    _rootJoint = (from j in Skeleton where j.ParentJoint == null select j).First();
                }
                return _rootJoint;
            }
        }

        internal void RasterizeFrame(double elapsedMilliseconds)
        {
            RasterizeFrame(elapsedMilliseconds, 0, 8);
        }

        public void RasterizeFrame(double commandTimeDifference, int earliestFrameIndex, int latestFrameIndex)
        {
            float earliestFrame = Timestamps[earliestFrameIndex];
            float latestFrame = Timestamps[latestFrameIndex];

            if (earliestFrameIndex != latestFrameIndex)
            {
                double currentFrame = commandTimeDifference % (int)((latestFrame - earliestFrame) * 1000) / 1000f + earliestFrame;
                rasterizedCurrentFrame = ((int)(currentFrame * Constants.INSTANCES_PER_SECOND) / Constants.INSTANCES_PER_SECOND);
            }
            else
            {
                rasterizedCurrentFrame = Timestamps[earliestFrameIndex];
            }
        }

        private float rasterizedCurrentFrame;

        private int latestFrameIndex;
        private DescriptorSet texelDescriptorSet;

        private float[] Timestamps
        {
            get
            {
                if (_timestamps == null)
                {
                    _timestamps = Skeleton.First().PoseOrigins.Keys.ToArray();
                }
                return _timestamps;
            }
        }

        public int Frames { get; set; }


        public AnimatedMesh(Mesh mesh) : base(mesh)
        {
            Name = mesh.Name;

            VertexBuffer = mesh.VertexBuffer;
            TextureCoordinateBuffer = mesh.TextureCoordinateBuffer;
            NormalBuffer = mesh.NormalBuffer;
            TangentBuffer = mesh.TangentBuffer;
            BitangentBuffer = mesh.BitangentBuffer;

            TextureManager.Textures = mesh.TextureManager.Textures;

            Material = mesh.Material;
        }

    
        public List<Joint> RasterizeAnimation()
        {
            List<Joint> theSkeleton = new List<Joint>();
            for (int i = 0; i < Skeleton.Count; i++)
            {
                theSkeleton.Add(new Joint());
                theSkeleton[i].Sid = Skeleton[i].Sid;
            }

            float maximumTimestamp = (float)(Math.Ceiling(Timestamps.Max() * Constants.INSTANCES_PER_SECOND) / Constants.INSTANCES_PER_SECOND);


            for (float rasterizedCurrentFrame = 0f; rasterizedCurrentFrame <= maximumTimestamp; rasterizedCurrentFrame += 1f / Constants.INSTANCES_PER_SECOND)
            {
                float smallerTimestamp = (from timestamp in Timestamps
                                          where timestamp <= rasterizedCurrentFrame
                                          select timestamp).Max();

                float biggerTimestamp = smallerTimestamp;

                List<float> biggerTimestamps = (from timestamp in Timestamps
                                                where timestamp >= rasterizedCurrentFrame
                                                select timestamp).ToList();
                if (biggerTimestamps.Count > 0)
                {
                    biggerTimestamp = biggerTimestamps.Min();
                }


                float blend = 0f;
                if (smallerTimestamp != biggerTimestamp)
                {
                    blend = (rasterizedCurrentFrame - smallerTimestamp) / (biggerTimestamp - smallerTimestamp);
                }

                InterpolateSkeleton(smallerTimestamp, biggerTimestamp, blend);
                InterpolateSkeletonQuaternions(smallerTimestamp, biggerTimestamp, blend);


                for (int i = 0; i < Skeleton.Count; i++)
                {
                    theSkeleton[i].PoseOrigins[rasterizedCurrentFrame] = Skeleton[i].InterpolatedPoseOrigin;
                    theSkeleton[i].PoseQuaternions[rasterizedCurrentFrame] = Skeleton[i].InterpolatedQuaternion;
                }
            }
            latestFrameIndex = Timestamps.Count() - 1;

            return theSkeleton;
        }

        private void InterpolateSkeletonQuaternions(float timeV1, float timeV2, float blend)
        {
            for (int i = 0; i < Skeleton.Count(); i++)
            {
                Skeleton[i].InterpolatedQuaternion = Quaternion.Slerp(Skeleton[i].PoseQuaternions[timeV1], Skeleton[i].PoseQuaternions[timeV2], blend);
            }
        }

        private void InterpolateSkeleton(float timeV1, float timeV2, float blend)
        {
            //Reset
            for (int i = 0; i < Skeleton.Count; i++)
            {
                Skeleton[i].InterpolatedPoseOrigin = new Vector3();
            }
            foreach (Joint joint in Skeleton)
            {
                joint.InterpolatedPoseOrigin = new Vector3();
            }

            // only linearly interpolate for root bone
            Vector3 lerpDirection = RootJoint.PoseOrigins[timeV2] - RootJoint.PoseOrigins[timeV1];

            lerpDirection *= blend;
            lerpDirection += RootJoint.PoseOrigins[timeV1];
            ApplyTranslationToJointAndChildren(RootJoint, lerpDirection);


            for (int i = 0; i < RootJoint.ChildJoints.Count; i++)
            {
                InterpolateSkeletonInternal(RootJoint.ChildJoints[i], timeV1, timeV2, blend);
            }
        }

        private void InterpolateSkeletonInternal(Joint joint, float timeV1, float timeV2, float blend)
        {
            Vector3 v1Direction = (joint.PoseOrigins[timeV1] - joint.ParentJoint.PoseOrigins[timeV1]);
            Vector3 v2Direction = (joint.PoseOrigins[timeV2] - joint.ParentJoint.PoseOrigins[timeV2]);

            float interpolatedLength = v1Direction.Length + (v2Direction.Length - v1Direction.Length) * blend;

            Vector3 lerpDirection = Vector3.Lerp(v1Direction, v2Direction, blend);
            lerpDirection.Normalize();

            lerpDirection *= interpolatedLength;

            ApplyTranslationToJointAndChildren(joint, lerpDirection);

            for (int i = 0; i < joint.ChildJoints.Count; i++)
            {
                InterpolateSkeletonInternal(joint.ChildJoints[i], timeV1, timeV2, blend);
            }
        }

        private void ApplyTranslationToJointAndChildren(Joint joint, Vector3 lerpDirection)
        {
            joint.InterpolatedPoseOrigin += lerpDirection;
            for (int i = 0; i < joint.ChildJoints.Count; i++)
            {
                ApplyTranslationToJointAndChildren(joint.ChildJoints[i], lerpDirection);
            }
        }
        
       


        public void InstanceAnimation()
        {
            List<Joint> theList = RasterizeAnimation();

            List<Vector4> quaternions = new List<Vector4>();
            List<Vector4> origins = new List<Vector4>();
            for (int i = 0; i < theList.Count; i++)
            {
                for (int j = 0; j < theList[i].PoseQuaternions.Keys.Count; j++)
                {

                    Quaternion quaternion = theList[i].PoseQuaternions[theList[i].PoseQuaternions.Keys.ElementAt(j)] * Skeleton[i].InverseBindQuaternion;
                    quaternions.Add(new Vector4(quaternion.Xyz, quaternion.W));

                    Vector3 origin = theList[i].PoseOrigins[theList[i].PoseOrigins.Keys.ElementAt(j)];
                    origins.Add(new Vector4(origin, 1));
                }
            }

            Frames = theList.First().PoseQuaternions.Keys.Count;
            InterpolatedQuaternionBuffer.Data = quaternions.ToArray();
            InterpolatedQuaternionBuffer.Initialize();
            InterpolatedOriginBuffer.Data = origins.ToArray();

            InterpolatedOriginBuffer.Initialize();
        }        
    }
}
