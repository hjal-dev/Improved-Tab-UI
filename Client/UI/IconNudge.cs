using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedTabUI.UI
{
    internal sealed class IconNudge : BaseMeshEffect
    {
        private static readonly List<UIVertex> Vertices = new List<UIVertex>();

        private float _offset;

        internal float Offset
        {
            get { return _offset; }
            set
            {
                if (Mathf.Approximately(_offset, value))
                {
                    return;
                }

                _offset = value;

                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || _offset == 0f)
            {
                return;
            }

            Vertices.Clear();
            vertices.GetUIVertexStream(Vertices);

            for (int i = 0; i < Vertices.Count; i++)
            {
                UIVertex vertex = Vertices[i];
                vertex.position.x += _offset;
                Vertices[i] = vertex;
            }

            vertices.Clear();
            vertices.AddUIVertexTriangleStream(Vertices);
        }
    }
}
