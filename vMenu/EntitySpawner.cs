using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;

using static CitizenFX.Core.Native.API;

namespace vMenuClient
{
    public class EntitySpawner : BaseScript
    {
        public static bool Active { get; private set; } = false;
        public static Entity CurrentEntity { get; private set; } = null;
        private int scaleform = 0;

        public static bool SetSpawnNetworked(bool value)
        {
            if (Active)
            {
                return false;
            }

            spawnNetworked = value;
            return true;
        }

        public enum RotationReference
        {
            Global,
            Camera,
            Object,
        }

        public enum RotationAxis
        {
            Pitch,
            Roll,
            Yaw,
        }

        public enum RotationDirection
        {
            Forward,
            Backward,
        }

        public static bool PlaceOnGround { get; set; } = true;
        public static bool AlignToSurfaceContinuously { get; set; } = false;
        public static float PlacementDistance { get; set; } = 20f;
        public static Quaternion PlacementRotation { get; set; } = Quaternion.Identity;

        public static bool IsEntityPositionFixed { get; set; } = false;

        public static RotationAxis RotationAxis_ { get; set; } = RotationAxis.Yaw;
        public static RotationReference RotationReference_ { get; set; } = RotationReference.Object;

        private static Quaternion? lastRotateEntityWithCameraYawQuat = null;
        private static bool rotateEntityWithCameraYaw = true;
        public static bool RotateEntityWithCameraYaw
        {
            get
            {
                return rotateEntityWithCameraYaw;
            }
            set
            {
                if (value != rotateEntityWithCameraYaw)
                {
                    lastRotateEntityWithCameraYawQuat = value ? null : CameraCoordinateSystemQuat;

                    var adjustQuat = CameraCoordinateSystemQuat;
                    if (!value)
                    {
                        adjustQuat = Quaternion.Invert(adjustQuat);
                    }

                    PlacementRotation = PlacementRotation * adjustQuat;
                }
                rotateEntityWithCameraYaw = value;
            }
        }

        public static bool SpawnDynamic { get; set; }


        private static bool spawnNetworked;
        private static Stack<bool> wereSpawnsNetworked = new Stack<bool>();
        private static Stack<Entity> localEntities = new Stack<Entity>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public EntitySpawner()
        {
#if DEBUG
            RegisterCommand("testEntity", new Action<int, List<object>>((source, args) =>
            {
                var prop = (string)args[0];
                SpawnEntity(prop, Game.PlayerPed.Position);
            }), false);

            RegisterCommand("endTest", new Action(() =>
            {
                FinishPlacement();
            }), false);
#endif
        }

        #region PublicMethods

        /// <summary>
        /// Method for spawning entity with EntitySpawner. After entity is spawned you will be able to change
        /// position of entity with your mouse.
        /// </summary>
        /// <param name="model">model of entity as string</param>
        /// <param name="coords">initial coords for the entity</param>
        /// <returns>true spawn was succesful</returns>
        public static void SpawnEntity(string model, Vector3 coords)
        {
            uint modelHash = 0;

            if (uint.TryParse(model, out var resultUint))
            {
                modelHash = resultUint;
            }
            else if (int.TryParse(model, out var resultInt))
            {
                modelHash = (uint)resultInt;
            }
            else
            {
                modelHash = (uint)GetHashKey(model);
            }
            SpawnEntity(modelHash, coords);
        }

        /// <summary>
        /// Method for spawning entity with EntitySpawner. After entity is spawned you will be able to change
        /// position of entity with your mouse.
        /// </summary>
        /// <param name="model">model of entity as hash</param>
        /// <param name="coords">initial coords for the entity</param>
        /// <returns>true spawn was succesful</returns>
        public static async void SpawnEntity(uint model, Vector3 coords)
        {
            if (!IsModelValid(model))
            {
                Notify.Error(CommonErrors.InvalidInput);
                return;
            }

            if (CurrentEntity != null)
            {
                Notify.Error("One entity is currently being processed.");
                return;
            }

            int handle;
            RequestModel(model);
            while (!HasModelLoaded(model))
            {
                await Delay(1);
            }
            if (IsModelAPed(model))
            {
                handle = CreatePed(4, model, coords.X, coords.Y, coords.Z, Game.PlayerPed.Heading, spawnNetworked, true);
            }
            else if (IsModelAVehicle(model) && !spawnNetworked)
            {
                handle = await CommonFunctions.SpawnVehicle(
                    model,
                    false,
                    false,
                    skipLoad: false,
                    vehicleInfo: new CommonFunctions.VehicleInfo(),
                    saveName: null,
                    coords.X,
                    coords.Y,
                    coords.Z,
                    Game.PlayerPed.Heading,
                    destructible: false,
                    upgraded: false,
                    withSavedModifications: false);
            }
            else
            {
                handle = CreateObject((int)model, coords.X, coords.Y, coords.Z, spawnNetworked, true, true);
            }

            CurrentEntity = Entity.FromHandle(handle);

            if (!CurrentEntity.Exists())
            {
                Notify.Error("Failed to create entity");
                return;
            }

            SetEntityAsMissionEntity(handle, true, true); // Set As mission to prevent despawning

            Active = true;
        }

        /// <summary>
        /// Method used to confirm location of prop and finish placement
        /// </summary>
        public static async void FinishPlacement(bool duplicate = false)
        {
            if (CurrentEntity != null)
            {
                if (spawnNetworked)
                {
                    TriggerServerEvent("vMenu:EntitySpawnerAdd", CurrentEntity.NetworkId);
                }
                else
                {
                    localEntities.Push(CurrentEntity);
                }
                wereSpawnsNetworked.Push(spawnNetworked);
            }

            if (duplicate)
            {
                var hash = CurrentEntity.Model.Hash;
                var position = CurrentEntity.Position;
                CurrentEntity = null;
                await Delay(1); // Mandatory
                SpawnEntity((uint)hash, position);
            }
            else
            {
                Active = false;
                CurrentEntity = null;
                ResetRotation();
            }
        }

        public static void RemoveMostRecent()
        {
            if (wereSpawnsNetworked.Count == 0)
            {
                return;
            }

            bool wasNetworked = wereSpawnsNetworked.Pop();
            if (wasNetworked)
            {
                TriggerServerEvent("vMenu:EntitySpawnerRemoveMostRecent");
            }
            else
            {
                var entity = localEntities.Pop();
                if (entity != null && entity.Exists())
                {
                    entity.Delete();
                }
            }
        }
        public static void RemoveAll()
        {
            foreach (var entity in localEntities)
            {
                if (entity != null && entity.Exists())
                {
                    entity.Delete();
                }
            }
            localEntities.Clear();
            TriggerServerEvent("vMenu:EntitySpawnerRemoveAll");
            wereSpawnsNetworked.Clear();
        }

        public async static Task CopyEntitiesToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var entity in localEntities)
            {
                sb.AppendLine(CommonFunctions.PrintEntityInfo(entity, false, true, true, false));
            }

            bool wait = true;
            TriggerServerEvent("vMenu:EntitySpawnerCopyToClipboard", CallbackFactory.Create((string text) =>
            {
                sb.Append(text);
                wait = false;
            }));

            while (wait)
            {
                await Delay(1);
            }

            CommonFunctions.CopyToClipboard(sb.ToString());
        }

        private static Quaternion RotationRollPitchYawDegQuat(Vector3 rot)
        {
            // Why are we dividing by 18.23684*pi you ask: I honestly don't know. 180.0*pi as we would normally do for
            // deg -> rad does not work. 18.23684 was simply determined by trial and error.
            const float CORRECTION_FACTOR = 1f / (float)(18.23684 * Math.PI);

            rot *= CORRECTION_FACTOR;

            // Quaternion.RotationYawPitchRoll name/param order is bogus, it is actually pitch (X), roll (Y), yaw (Z)
            return Quaternion.Normalize(Quaternion.RotationYawPitchRoll(rot.X, rot.Y, rot.Z));
        }

        public static Quaternion RotationToQuaternion(RotationAxis axis, RotationDirection direction, float amount)
        {
            amount = amount * (direction == RotationDirection.Forward ? 1f : -1f);
            Vector3 rot = new Vector3();
            switch (axis)
            {
                case RotationAxis.Roll:
                    rot = new Vector3(amount, 0, 0);
                    break;
                case RotationAxis.Pitch:
                    rot = new Vector3(0, amount, 0);
                    break;
                case RotationAxis.Yaw:
                    rot = new Vector3(0, 0, amount);
                    break;
            }

            return RotationRollPitchYawDegQuat(rot);
        }

        public static void RotateEntity(RotationDirection direction, float amount)
        {
            var rot = PlacementRotation;
            rot = rot * Quaternion.Invert(CameraCoordinateSystemQuatIfNeeded);

            var change = RotationToQuaternion(RotationAxis_, direction, amount);
            switch (RotationReference_)
            {
                case RotationReference.Global:
                    rot = rot * Quaternion.Invert(change);
                    break;
                case RotationReference.Camera:
                    rot = rot * CameraCoordinateSystemQuat * Quaternion.Invert(change) * Quaternion.Invert(CameraCoordinateSystemQuat);
                    break;
                case RotationReference.Object:
                    rot = change * rot;
                    break;
            }
            rot = rot * CameraCoordinateSystemQuatIfNeeded;

            PlacementRotation = rot;
        }

        public static void ResetRotation()
        {
            PlacementRotation = RotateEntityWithCameraYaw
                ? Quaternion.Identity
                : Quaternion.Invert(lastRotateEntityWithCameraYawQuat.HasValue
                ? lastRotateEntityWithCameraYawQuat.Value
                : CameraCoordinateSystemQuat);
        }
        #endregion

        #region InternalMethods

        /// <summary>
        /// Used internally for drawing of help text
        /// </summary>
        private void DrawButtons() //TODO: Right keys
        {
            BeginScaleformMovieMethod(scaleform, "CLEAR_ALL");
            EndScaleformMovieMethod();

            BeginScaleformMovieMethod(scaleform, "SET_DATA_SLOT");
            ScaleformMovieMethodAddParamInt(0);
            PushScaleformMovieMethodParameterString("~INPUT_VEH_FLY_ROLL_LR~");
            PushScaleformMovieMethodParameterString("Rotate Object");
            EndScaleformMovieMethod();

            BeginScaleformMovieMethod(scaleform, "DRAW_INSTRUCTIONAL_BUTTONS");
            ScaleformMovieMethodAddParamInt(0);
            EndScaleformMovieMethod();

            DrawScaleformMovieFullscreen(scaleform, 255, 255, 255, 255, 0);
        }

        /// <summary>
        /// Used internally for getting direction vector from rotation vector
        /// </summary>
        /// <param name="rotation">Input rotation vector</param>
        /// <returns>Output direction vector</returns>
        private static Vector3 RotationToDirection(Vector3 rotation)
        {
            var adj = new Vector3(
                (float)Math.PI / 180f * rotation.X,
                (float)Math.PI / 180f * rotation.Y,
                (float)Math.PI / 180f * rotation.Z
            );

            return new Vector3(
                (float)(-Math.Sin(adj.Z) * Math.Abs(Math.Cos(adj.X))),
                (float)(Math.Cos(adj.Z) * Math.Abs(Math.Cos(adj.X))),
                (float)Math.Sin(adj.X)
            );
        }

        /// <summary>
        /// Used to get coords of reycast from player camera;
        /// </summary>
        /// <returns>destination if no hit was found and coords of hit if there was one</returns>
        private Vector3 GetCoordsPlayerIsLookingAt()
        {
            var camRotation = GetGameplayCamRot(0);
            var camCoords = GetGameplayCamCoord();
            var camDirection = RotationToDirection(camRotation);

            var dist = PlaceOnGround ? 2000f : PlacementDistance;

            var dest = new Vector3(
                camCoords.X + (camDirection.X * dist),
                camCoords.Y + (camDirection.Y * dist),
                camCoords.Z + (camDirection.Z * dist)
            );

            if (PlaceOnGround)
            {
                var res = World.Raycast(camCoords, dest, IntersectOptions.Everything, Game.PlayerPed);

#if DEBUG
            DrawLine(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, dest.X, dest.Y, dest.Z, 255, 0, 0, 255);
#endif

                dest = res.DitHit ? res.HitPosition : dest;
            }

            return dest;
        }

        public static Quaternion CameraCoordinateSystemQuat =>
            RotationToQuaternion(RotationAxis.Yaw, RotationDirection.Forward, GetGameplayCamRot(0).Z);

        public static Quaternion CameraCoordinateSystemQuatIfNeeded
        {
            get
            {
                if (!RotateEntityWithCameraYaw)
                {
                    return Quaternion.Identity;
                }

                return CameraCoordinateSystemQuat;
            }
        }

        public static Quaternion FinalEntityRotationQuat
        {
            get
            {
                return CameraCoordinateSystemQuatIfNeeded * Quaternion.Invert(PlacementRotation);
            }
        }

        public static Quaternion ExtractPlacementRotation(Quaternion finalRotation)
        {
            return Quaternion.Invert(finalRotation) * CameraCoordinateSystemQuatIfNeeded;
        }

        public static Quaternion GetEntityQuat(Entity entity)
        {
            float x = 0f, y = 0f, z = 0f, w = 0f;
            GetEntityQuaternion(entity.Handle, ref x, ref y, ref z, ref w);
            return new Quaternion(x, y, z, w);
        }

        public static void SetEntityQuat(Entity entity, Quaternion quat)
        {
            SetEntityQuaternion(entity.Handle, quat.X, quat.Y, quat.Z, quat.W);
        }

        public static void AlignEntityToSurface()
        {
            if (CurrentEntity == null)
            {
                return;
            }

            var quatPre = GetEntityQuat(CurrentEntity);

            if (CurrentEntity.Model.IsVehicle)
            {
                SetVehicleOnGroundProperly(CurrentEntity.Handle);
            }
            else
            {
                PlaceObjectOnGroundProperly(CurrentEntity.Handle);
            }

            var quatPost = GetEntityQuat(CurrentEntity);

            var quat = Quaternion.RotationAxis(quatPost.Axis, quatPre.Angle);

            PlacementRotation = ExtractPlacementRotation(quat);
            SetEntityQuat(CurrentEntity, quat);
        }

        #endregion

        /// <summary>
        /// Main tick method for class
        /// </summary>
        [Tick]
        internal async Task MoveHandler()
        {
            if (Active)
            {
                scaleform = RequestScaleformMovie("INSTRUCTIONAL_BUTTONS");
                while (!HasScaleformMovieLoaded(scaleform))
                {
                    await Delay(0);
                }

                DrawScaleformMovieFullscreen(scaleform, 255, 255, 255, 0, 0);
            }
            else
            {
                if (scaleform != 0)
                {
                    SetScaleformMovieAsNoLongerNeeded(ref scaleform); // Unload scaleform if there is no need to draw it
                    scaleform = 0;
                }
            }

            while (Active)
            {
                if (CurrentEntity == null || !CurrentEntity.Exists())
                {
                    Active = false;
                    CurrentEntity = null;
                    break;
                }
                var handle = CurrentEntity.Handle;

                DrawButtons();

                FreezeEntityPosition(handle, true);
                SetEntityInvincible(handle, true);
                SetEntityCollision(handle, false, false);
                SetEntityAlpha(handle, (int)(255 * 0.4), 0);

                if (!IsEntityPositionFixed)
                {
                    var newPosition = GetCoordsPlayerIsLookingAt();
                    CurrentEntity.Position = newPosition;
                }

                if (PlaceOnGround && AlignToSurfaceContinuously && CurrentEntity.HeightAboveGround < 3.0f)
                {
                    AlignEntityToSurface();
                }

                SetEntityQuat(CurrentEntity, FinalEntityRotationQuat);

                await Delay(0);

                if (SpawnDynamic)
                {
                    FreezeEntityPosition(handle, false);
                    SetEntityInvincible(handle, false);
                }
                SetEntityCollision(handle, true, true);
                ResetEntityAlpha(handle);
            }

            await Task.FromResult(0);
        }

        internal static void DrawCoordinateLine(Vector3 direction, Vector3 objPos, Quaternion rotation, int red, int green, int blue, int alpha)
        {
            var rotatedDirection = Vector3.Normalize(Vector3.Transform(direction, rotation));
            var start = objPos - rotatedDirection;
            var end = objPos + rotatedDirection;

            DrawLine(
                start.X,
                start.Y,
                start.Z,
                end.X,
                end.Y,
                end.Z,
                red,
                green,
                blue,
                alpha);
        }

        [Tick]
        internal async Task DrawCoordinateSystem()
        {
            if (!Active || CurrentEntity == null || !CurrentEntity.Exists())
            {
                return;
            }

            Vector3 right = new Vector3(1, 0, 0);
            Vector3 forward = new Vector3(0, 1, 0);
            Vector3 up = new Vector3(0, 0, 1);

            var pos = CurrentEntity.Position;
            var rotation = new Quaternion();
            switch (RotationReference_)
            {
                case RotationReference.Camera:
                    rotation = CameraCoordinateSystemQuat;
                    break;
                case RotationReference.Global:
                    rotation = Quaternion.Identity;
                    break;
                case RotationReference.Object:
                    rotation = CameraCoordinateSystemQuatIfNeeded * Quaternion.Invert(PlacementRotation);
                    break;
            }

            void DrawCoordinateLine(Vector3 direction, int red, int green, int blue, int alpha)
            {
                EntitySpawner.DrawCoordinateLine(direction, pos, rotation, red, green, blue, alpha);
            }

            int GetAxisAlpha(RotationAxis axis)
            {
                return axis == RotationAxis_ ? 255 : 127;
            }

            DrawCoordinateLine(right, 255, 0, 0, GetAxisAlpha(RotationAxis.Pitch));
            DrawCoordinateLine(forward, 0, 255, 0, GetAxisAlpha(RotationAxis.Roll));
            DrawCoordinateLine(up, 0, 0, 255, GetAxisAlpha(RotationAxis.Yaw));

            await Task.FromResult(0);
        }
    }
}
