using System;
using System.Collections.Generic;

namespace VulkanBase.Collada
{
    public class Joint
    {
        public List<Joint> ChildJoints { get; set; }
        public string Sid { get; set; }
        public Vector3 Origin { get; set; }
        public Matrix4 TransformationMatrix { get; set; }
        public Matrix4 InverseBindMatrix { get; set; }
        public Quaternion InverseBindQuaternion { get; set; }

        public Quaternion TheQuaternion { get; set; }
        public List<float> Weight { get; set; }

        public Dictionary<float, Quaternion> PoseQuaternions;
        public Dictionary<float, Matrix4> PoseTransformations;
        public Dictionary<float, Vector3> PoseOrigins;

        public Vector3 InterpolatedPoseOrigin;
        public Quaternion InterpolatedQuaternion;
        public Matrix4 InterpolatedPoseTransformation;

        private Joint _parentJoint;

        public Joint()
        {
            Weight = new List<float>();
            PoseQuaternions = new Dictionary<float, Quaternion>();
            PoseTransformations = new Dictionary<float, Matrix4>();
            PoseOrigins = new Dictionary<float, Vector3>();
            ChildJoints = new List<Joint>();
        }

        public Joint ParentJoint
        {
            get
            {
                return _parentJoint;
            }
            set
            {
                _parentJoint = value;
                if (_parentJoint != null)
                {
                    if (_parentJoint.ChildJoints.Contains(this))
                    {
                        throw new Exception("Duplicate Joint found");
                    }
                    _parentJoint.ChildJoints.Add(this);
                }
            }
        }


        public override string ToString()
        {
            return Sid;
        }


    }
}