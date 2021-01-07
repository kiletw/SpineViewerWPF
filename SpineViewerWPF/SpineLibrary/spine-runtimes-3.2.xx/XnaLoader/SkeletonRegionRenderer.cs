/******************************************************************************
 * Spine Runtimes Software License
 * Version 2.3
 * 
 * Copyright (c) 2013-2015, Esoteric Software
 * All rights reserved.
 * 
 * You are granted a perpetual, non-exclusive, non-sublicensable and
 * non-transferable license to use, install, execute and perform the Spine
 * Runtimes Software (the "Software") and derivative works solely for personal
 * or internal use. Without the written permission of Esoteric Software (see
 * Section 2 of the Spine Software License Agreement), you may not (a) modify,
 * translate, adapt or otherwise create derivative works, improvements of the
 * Software or develop new applications using the Software or (b) remove,
 * delete, alter or obscure any trademarks or any copyright, trademark, patent
 * or other intellectual property or proprietary rights notices on or in the
 * Software, including any copy thereof. Redistributions in binary or source
 * form must include this license and terms.
 * 
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE "AS IS" AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL ESOTERIC SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Spine3_2_xx {
	/// <summary>Draws region attachments.</summary>
	public class SkeletonRegionRenderer {
		GraphicsDevice device;
		RegionBatcher batcher;
		RasterizerState rasterizerState;
		float[] vertices = new float[8];
		BlendState defaultBlendState;

		BasicEffect effect;
		public BasicEffect Effect { get { return effect; } set { effect = value; } }

		private bool premultipliedAlpha;
		public bool PremultipliedAlpha { get { return premultipliedAlpha; } set { premultipliedAlpha = value; } }

		public SkeletonRegionRenderer (GraphicsDevice device) {
			this.device = device;

			batcher = new RegionBatcher();

			effect = new BasicEffect(device);
			effect.World = Matrix.Identity;
			effect.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);
			effect.TextureEnabled = true;
			effect.VertexColorEnabled = true;

			rasterizerState = new RasterizerState();
			rasterizerState.CullMode = CullMode.None;

			Bone.yDown = true;
		}

		public void Begin () {
			defaultBlendState = premultipliedAlpha ? BlendState.AlphaBlend : BlendState.NonPremultiplied;

			device.RasterizerState = rasterizerState;
			device.BlendState = defaultBlendState;

			effect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 1, 0);
		}

		public void End () {
			foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
				pass.Apply();
				batcher.Draw(device);
			}
		}

		public void Draw (Skeleton skeleton) {
			var drawOrder = skeleton.DrawOrder;
			var drawOrderItems = skeleton.DrawOrder.Items;
			float skeletonR = skeleton.R, skeletonG = skeleton.G, skeletonB = skeleton.B, skeletonA = skeleton.A;
			for (int i = 0, n = drawOrder.Count; i < n; i++) {
				Slot slot = drawOrderItems[i];
				RegionAttachment regionAttachment = slot.Attachment as RegionAttachment;
				if (regionAttachment != null) {
                    BlendState blendState = new BlendState();
                    Blend blendSrc;
                    Blend blendDst;
                    if (premultipliedAlpha)
                    {
                        blendState.AlphaBlendFunction = BlendState.AlphaBlend.AlphaBlendFunction;
                        blendState.BlendFactor = BlendState.AlphaBlend.BlendFactor;
                        blendState.ColorBlendFunction = BlendState.AlphaBlend.ColorBlendFunction;
                        blendState.ColorWriteChannels = BlendState.AlphaBlend.ColorWriteChannels;
                        blendState.ColorWriteChannels1 = BlendState.AlphaBlend.ColorWriteChannels1;
                        blendState.ColorWriteChannels2 = BlendState.AlphaBlend.ColorWriteChannels2;
                        blendState.ColorWriteChannels3 = BlendState.AlphaBlend.ColorWriteChannels3;
                        blendState.MultiSampleMask = BlendState.AlphaBlend.MultiSampleMask;
                    }
                    else
                    {
                        blendState.AlphaBlendFunction = BlendState.NonPremultiplied.AlphaBlendFunction;
                        blendState.BlendFactor = BlendState.NonPremultiplied.BlendFactor;
                        blendState.ColorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction;
                        blendState.ColorWriteChannels = BlendState.NonPremultiplied.ColorWriteChannels;
                        blendState.ColorWriteChannels1 = BlendState.NonPremultiplied.ColorWriteChannels1;
                        blendState.ColorWriteChannels2 = BlendState.NonPremultiplied.ColorWriteChannels2;
                        blendState.ColorWriteChannels3 = BlendState.NonPremultiplied.ColorWriteChannels3;
                        blendState.MultiSampleMask = BlendState.NonPremultiplied.MultiSampleMask;
                    }
                    switch (slot.Data.BlendMode)
                    {
                        case BlendMode.additive:
                            blendState = BlendState.Additive;
                            break;
                        case BlendMode.multiply:
                            blendSrc = BlendXna.GetXNABlend(BlendXna.GL_DST_COLOR);
                            blendDst = BlendXna.GetXNABlend(BlendXna.GL_ONE_MINUS_SRC_ALPHA);
                            blendState.ColorSourceBlend = blendSrc;
                            blendState.AlphaSourceBlend = blendSrc;
                            blendState.ColorDestinationBlend = blendDst;
                            blendState.AlphaDestinationBlend = blendDst;
                            break;
                        case BlendMode.screen:
                            blendSrc = BlendXna.GetXNABlend(premultipliedAlpha ? BlendXna.GL_ONE : BlendXna.GL_SRC_ALPHA);
                            blendDst = BlendXna.GetXNABlend(BlendXna.GL_ONE_MINUS_SRC_COLOR);
                            blendState.ColorSourceBlend = blendSrc;
                            blendState.AlphaSourceBlend = blendSrc;
                            blendState.ColorDestinationBlend = blendDst;
                            blendState.AlphaDestinationBlend = blendDst;
                            break;
                        default:
                            blendState = defaultBlendState;
                            break;
                    }
                    if (device.BlendState != blendState)
                    {
                        End();
                        device.BlendState = blendState;
                    }

                    RegionItem item = batcher.NextItem();

					AtlasRegion region = (AtlasRegion)regionAttachment.RendererObject;
					item.texture = (Texture2D)region.page.rendererObject;

					Color color;
					float a = skeletonA * slot.A;
					if (premultipliedAlpha)
						color = new Color(skeletonR * slot.R * a, skeletonG * slot.G * a, skeletonB * slot.B * a, a);
					else
						color = new Color(skeletonR * slot.R, skeletonG * slot.G, skeletonB * slot.B, a);
					item.vertexTL.Color = color;
					item.vertexBL.Color = color;
					item.vertexBR.Color = color;
					item.vertexTR.Color = color;

					float[] vertices = this.vertices;
					regionAttachment.ComputeWorldVertices(slot.Bone, vertices);
					item.vertexTL.Position.X = vertices[RegionAttachment.X1];
					item.vertexTL.Position.Y = vertices[RegionAttachment.Y1];
					item.vertexTL.Position.Z = 0;
					item.vertexBL.Position.X = vertices[RegionAttachment.X2];
					item.vertexBL.Position.Y = vertices[RegionAttachment.Y2];
					item.vertexBL.Position.Z = 0;
					item.vertexBR.Position.X = vertices[RegionAttachment.X3];
					item.vertexBR.Position.Y = vertices[RegionAttachment.Y3];
					item.vertexBR.Position.Z = 0;
					item.vertexTR.Position.X = vertices[RegionAttachment.X4];
					item.vertexTR.Position.Y = vertices[RegionAttachment.Y4];
					item.vertexTR.Position.Z = 0;

					float[] uvs = regionAttachment.UVs;
					item.vertexTL.TextureCoordinate.X = uvs[RegionAttachment.X1];
					item.vertexTL.TextureCoordinate.Y = uvs[RegionAttachment.Y1];
					item.vertexBL.TextureCoordinate.X = uvs[RegionAttachment.X2];
					item.vertexBL.TextureCoordinate.Y = uvs[RegionAttachment.Y2];
					item.vertexBR.TextureCoordinate.X = uvs[RegionAttachment.X3];
					item.vertexBR.TextureCoordinate.Y = uvs[RegionAttachment.Y3];
					item.vertexTR.TextureCoordinate.X = uvs[RegionAttachment.X4];
					item.vertexTR.TextureCoordinate.Y = uvs[RegionAttachment.Y4];
				}
			}
		}
	}
}
