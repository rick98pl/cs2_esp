using BasicESPTutorial;
using Swed64;
using System.Numerics;
using System.Reflection.PortableExecutable;

Swed swed = new Swed("cs2");
IntPtr client = swed.GetModuleBase("client.dll");
Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));
renderThread.Start();

Vector2 screenSize = renderer.screenSize;
List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();

int dwEntityList = 0x1CBE6A0;
int dwViewMatrix = 0x1D21A80;
int dwLocalPlayerPawn = 0x1AF4B80;

int m_vOldOrigin = 0x15B0;
int m_iTeamNum = 0x3EB;
int m_lifeState = 0x350;
int m_hPlayerPawn = 0x8FC;

int m_vecViewOffset = 0xD98;
int m_modelState = 0x170;
int m_pGameSceneNode = 0x330;


while (true)
{
    entities.Clear();
    IntPtr entityList = swed.ReadPointer(client, dwEntityList);
    IntPtr listEntry = swed.ReadPointer(entityList, 0x10);
    IntPtr localPlayerPawn = swed.ReadPointer(client, dwLocalPlayerPawn);
    localPlayer.team = swed.ReadInt(localPlayerPawn, m_iTeamNum);

    for(int i = 0; i < 64; i++)
    {
        IntPtr currentController = swed.ReadPointer(listEntry, i * 0x78);
        if (currentController == IntPtr.Zero)
        {
            continue;
        }

        int pawnHandle = swed.ReadInt(currentController, m_hPlayerPawn);
        if (pawnHandle == 0)
        {
            continue;
        }

        IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
        if (listEntry2 == IntPtr.Zero) continue;
        IntPtr currentPawn = swed.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));
        if (currentPawn == IntPtr.Zero) continue;

        uint lifeState = swed.ReadUInt(currentPawn, m_lifeState);

        if (lifeState != 256)
            continue;

        float[] viewMatrix = swed.ReadMatrix(client + dwViewMatrix);

        IntPtr sceneNode = swed.ReadPointer(currentPawn, m_pGameSceneNode);
        IntPtr boneMatrix = swed.ReadPointer(sceneNode, m_modelState + 0x80);
        Entity entity = new Entity();
        entity.team = swed.ReadInt(currentPawn, m_iTeamNum);
        entity.position = swed.ReadVec(currentPawn, m_vOldOrigin); 
        entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
        entity.viewPosition2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewOffset), screenSize);
        entity.distance = Vector3.Distance(entity.position, localPlayer.position);
        entity.bones = Calculate.ReadBones(boneMatrix, swed);
        entity.bones2d = Calculate.ReadBones2D(entity.bones, viewMatrix, screenSize);
        entities.Add(entity);
    }

    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntities(entities);

}