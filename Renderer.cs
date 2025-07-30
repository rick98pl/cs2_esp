using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ClickableTransparentOverlay;
using ImGuiNET;
using System.Windows.Forms;

namespace BasicESPTutorial
{
    internal class Renderer : Overlay
    {
        // screenSize will be set dynamically
        public Vector2 screenSize;

        public ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        private bool enableESP = true;
        private Vector4 enemyColor = new Vector4(1, 0, 0, 1);
        private Vector4 teamColor = new Vector4(0, 1, 0, 1);
        private Vector4 boneColor = new Vector4(1, 1, 1, 1);
        float boneThickness = 4;



        ImDrawListPtr drawList;

        public Renderer()
        {
            // Get the primary display resolution dynamically
            var bounds = Screen.PrimaryScreen.Bounds;
            screenSize = new Vector2(bounds.Width, bounds.Height);
        }

        protected override void Render()
        {
            ImGui.Begin("Basic ESP");
            ImGui.Checkbox("Enable ESP", ref enableESP);

            if (ImGui.CollapsingHeader("Team color"))
            {
                ImGui.ColorEdit4("##teamcolor", ref teamColor);
            }

            if (ImGui.CollapsingHeader("Enemy color"))
            {
                ImGui.ColorEdit4("##enemycolor", ref enemyColor);
            }

            //if (ImGui.CollapsingHeader("Bone color"))
            //{
            //    ImGui.ColorEdit4("##bonecolor", ref enemyColor);
            //}

            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        
                        DrawLine(entity);
                        DrawBones(entity);
                        DrawBox(entity);
                    }
                }
            }
        }

        bool EntityOnScreen(Entity entity)
        {
            if (entity.position2D.X > 0 && entity.position2D.X < screenSize.X && entity.position2D.Y > 0 && entity.position2D.Y < screenSize.Y)
            {
                return true;
            }
            return false;
        }

        private void DrawBones(Entity entity)
        {
            Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            Vector4 black = new Vector4(0, 0, 0, 1);

            if (lineColor != black)
            {
                uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);

                float currentBoneThickness = boneThickness / entity.distance;
                // draw Lines between bones
                drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness); // neck to head
                drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness); // neck to left shoulder
                drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness); // neck to shoulderRight
                drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness); // shoulderLeft to armLeft
                drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness); // shoulderRight to armRight
                drawList.AddLine(entity.bones2d[4], entity.bones2d[5], uintColor, currentBoneThickness); // armLeft to handLeft
                drawList.AddLine(entity.bones2d[7], entity.bones2d[8], uintColor, currentBoneThickness); // armRight to handRight
                drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness); // neck to waist
                drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness); // waist to kneeLeft
                drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness); // waist to kneeRight
                drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness); // kneeLeft to feetLeft
                drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness); // kneeRight to feetRight

                drawList.AddCircle(entity.bones2d[2], 3 + currentBoneThickness, uintColor);
            }

          
        }

        private void DrawBox(Entity entity)
        {
            Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            Vector4 black = new Vector4(0, 0, 0, 1);

            if (lineColor != black)
            {
                Vector2 head = entity.bones2d[2];

                Vector2 leftFoot = entity.bones2d[10];
                Vector2 rightFoot = entity.bones2d[12];
                float feetY = Math.Max(leftFoot.Y, rightFoot.Y);

                Vector2 leftHand = entity.bones2d[5];
                Vector2 rightHand = entity.bones2d[8];
                float minX = Math.Min(head.X, Math.Min(leftHand.X, rightHand.X));
                float maxX = Math.Max(head.X, Math.Max(leftHand.X, rightHand.X));
                minX = Math.Min(minX, Math.Min(entity.bones2d[3].X, entity.bones2d[6].X));
                maxX = Math.Max(maxX, Math.Max(entity.bones2d[3].X, entity.bones2d[6].X));

                float boxHeight = feetY - head.Y;
                float boxWidth = maxX - minX;

                float extraHeight = boxHeight * 0.3f;
                float extraWidth = boxWidth * 0.6f;

                Vector2 topLeft = new Vector2(minX - extraWidth / 2, head.Y - extraHeight / 2);
                Vector2 bottomRight = new Vector2(maxX + extraWidth / 2, feetY + extraHeight / 2);

                Vector4 boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;
                drawList.AddRect(topLeft, bottomRight, ImGui.ColorConvertFloat4ToU32(boxColor), 0f, ImDrawFlags.None, 2.5f);

            }
        }

        public void DrawLine(Entity entity)
        {
            Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            Vector4 black = new Vector4(0, 0, 0, 1);

            if (lineColor != black)
            {
                drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor));
            }
            
        }

        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }

        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entityLock)
            {
                localPlayer = newEntity;
            }
        }

        public Entity GetLocalPlayer()
        {
            lock (entityLock)
            {
                return localPlayer;
            }
        }

        void DrawOverlay(Vector2 screenSize)
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin("overlay", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
            );
        }
    }
}