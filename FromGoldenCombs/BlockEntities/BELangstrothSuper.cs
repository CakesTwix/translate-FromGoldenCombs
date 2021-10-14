﻿using FromGoldenCombs.Blocks;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace FromGoldenCombs.BlockEntities
{
    //TODO: Consider adding a lid object, or adding an animation showing the lid being slid off (This sounds neat). 
    //TODO: Find out how to get animation functioning
    //TODO: Fix selection box issue
    
    class BELangstrothSuper : BlockEntityDisplay
    {

        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "langstrothsuper";

        Block block;

        public BELangstrothSuper()
        {
            inv = new InventoryGeneric(10, "frameslot-0", null, null);
            meshes = new MeshData[10];
        }

        public override void Initialize(ICoreAPI api)
        {
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);
        }
                
        public override void OnBlockBroken()
        {
            // Don't drop inventory contents
        }

        BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
        }
        Vec3f rendererRot = new Vec3f();

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            CollectibleObject colObj = slot.Itemstack?.Collectible;
            bool isBeeframe = colObj?.Attributes != null && colObj.Attributes["beeframe"].AsBool(false) == true;
            BlockContainer block = Api.World.BlockAccessor.GetBlock(blockSel.Position) as BlockContainer;
            block.SetContents(new ItemStack(block), this.GetContentStacks());
            ItemStack stack = new ItemStack(block);
                        
            if ((slot.Empty || !isBeeframe) && blockSel.SelectionBoxIndex < 10 && this.Block.Variant["open"] == "open")
            {
                if (TryTake(byPlayer, blockSel))
                {
                    return true;
                }
            }
            else if (isBeeframe && blockSel.SelectionBoxIndex < 10  && this.Block.Variant["open"] == "open")
            {
                //AssetLocation sound = slot.Itemstack?.Item?.Sounds?.Place;
                if (TryPut(slot, blockSel))
                {
                    //Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    return true;
                }

            }
            else if (byPlayer.Entity.Controls.Sneak
                     && slot.Itemstack == null
                     && (int)slot.StorageType == 2
                     && Api.World.BlockAccessor.GetBlock(blockSel.Position).Variant["open"] == "closed"
                     && byPlayer.InventoryManager.TryGiveItemstack(block.OnPickBlock(Api.World, blockSel.Position))
                     )
            {
                    Api.World.BlockAccessor.SetBlock(0, blockSel.Position);
                    return true;        
            }
            else if (   //byPlayer.Entity.Controls.Sneak &&
                        !slot.Empty && 
                        slot.Itemstack.Block is LangstrothSuper &&
                        slot.Itemstack.Collectible.Variant["open"]  == "closed" &&
                        this.Block.Variant["open"]=="closed")
            {
                string side = getSide(block);
                ItemStack held = slot.Itemstack;
                ItemStack super = block.OnPickBlock(Api.World, blockSel.Position);
                Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(new AssetLocation("fromgoldencombs", "langstrothstack-two-" + getSide(block))).BlockId, blockSel.Position);
                BELangstrothStack lStack = (BELangstrothStack)Api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                lStack.InitializePut(super, held);
                slot.TakeOutWhole();
                return true;
            }
            else if (this.Block.Variant["open"] == "open" && !byPlayer.Entity.Controls.Sneak)
            {
                
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(new AssetLocation("fromgoldencombs", "langstrothsuper-closed-"+ getSide(block))).BlockId, blockSel.Position);
                this.MarkDirty();
                return true;
            }
            else if (this.Block.Variant["open"] == "closed")
            {
                Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(new AssetLocation("fromgoldencombs", "langstrothsuper-open-"+ getSide(block))).BlockId, blockSel.Position);
                return true;
            }
            return false;
        }

        private string getSide(Block block)
        {
            return Api.World.BlockAccessor.GetBlock(this.block.BlockId).Variant["side"].ToString();
        }

        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            int index = blockSel.SelectionBoxIndex;

            for (int i = 0; i < inv.Count; i++)
            {
                int slotnum = (index + i) % inv.Count;
                if (inv[slotnum].Empty)
                {
                    int moved = slot.TryPutInto(Api.World, inv[slotnum]);
                    updateMeshes();
                    MarkDirty(true);
                    return moved > 0;
                }
            }

            return false;
        }

        private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            int index = blockSel.SelectionBoxIndex;

            if (!inv[index].Empty)
            {
                ItemStack stack = inv[index].TakeOut(1);
                if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }

                if (stack.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                updateMeshes();
                MarkDirty(true);
                return true;
            }

            return false;
        }


        readonly Matrixf mat = new();

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mat.Identity();
            mat.RotateYDeg(block.Shape.rotateY);

            return base.OnTesselation(mesher, tessThreadTesselator);
        }

        protected override void updateMeshes()
        {
            mat.Identity();
            mat.RotateYDeg(block.Shape.rotateY);

            base.updateMeshes();
        }

        protected override MeshData genMesh(ItemStack stack, int index)
        {
            MeshData mesh;

            ICoreClientAPI capi = Api as ICoreClientAPI;
                nowTesselatingItem = stack.Item;
                nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                capi.Tesselator.TesselateItem(stack.Item, out mesh, this);

                mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);

            float x = 0;
            float y = 0;
            float z = 0;

            if (block.Variant["side"] == "north")
            {
                x = .7253f + .0625f * index - 1;
                y = .069f;
                z = 0;
                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            } else if (block.Variant["side"] == "south")
            {
                x = 1.2878f - .0625f * index - 1;
                y = .069f;
                z = 0;
                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            } else if (block.Variant["side"] == "east")
            {
                x = 0f;
                y = .069f;
                z = 1.2878f - .0625f * index - 1;
                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            }
            else if (block.Variant["side"] == "west")
            {
                x = 0f;
                y = .069f;
                z = .7253f + .0625f * index - 1;
                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            }
            ModelTransform transform = stack.Collectible.Attributes.AsObject<ModelTransform>();
            transform.EnsureDefaultValues();
            transform.Rotation.X = 0;
            transform.Rotation.Y = block.Shape.rotateY;
            transform.Rotation.Z = 0;
            mesh.ModelTransform(transform);

            return mesh;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
            if (forPlayer.CurrentBlockSelection == null)
            {
                base.GetBlockInfo(forPlayer, sb);
            }
            else if (this.Block.Variant["open"] == "closed")
            {
                return;
            } else if (index == 10)
            {

                for (int i = 0; i < 10; i++)
                {
                    ItemSlot slot = inv[i];
                    if (slot.Empty)
                    {
                        sb.AppendLine(Lang.Get("Empty"));
                    }
                    else
                    {
                        sb.AppendLine(slot.Itemstack.GetName());
                    }
                }
            }
            else if (index < 10)
            {
                ItemSlot slot = inv[index];
                if (slot.Empty)
                {
                    sb.AppendLine(Lang.Get("Empty"));
                }
                else
                {
                    sb.AppendLine(slot.Itemstack.GetName());
                }
            }
        }
    }
}

