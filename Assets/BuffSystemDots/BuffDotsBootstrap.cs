using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff DOTS 启动引导 — 负责初始化 World、创建配置 BlobAsset、注册到 Manager
    /// </summary>
    public class BuffDotsBootstrap : MonoBehaviour
    {
        [Header("Buff 配置资产")]
        [Tooltip("所有 Buff 配置数据资产")]
        public BuffConfigData[]                                     buff_configs_;

        [Header("特效预制体（可选）")]          
        [Tooltip("特效预制体列表")]         
        public GameObject[]                                         effect_prefabs_;

        private World                                               default_world_;
        private BlobAssetReference<BuffConfigBlobArray>             config_blob_;

        private void Start()
        {
            InitializeWorldAndConfigs();
        }

        private void OnDestroy()
        {
            DisposeBlobAssets();
        }

        private void InitializeWorldAndConfigs()
        {
            // 获取默认 World
            default_world_ = World.DefaultGameObjectInjectionWorld;
            if (default_world_ == null || !default_world_.IsCreated)
            {
                Debug.LogError("Default World is not created. Ensure DOTS systems are properly set up.");
                return;
            }

            var entity_manager = default_world_.EntityManager;

            // 创建配置 BlobAsset
            CreateConfigBlobAsset();
            if (!config_blob_.IsCreated)
            {
                Debug.LogError("Failed to create BuffConfigBlobAsset.");
                return;
            }

            // 创建单例实体持有配置注册表
            Entity singleton_entity = entity_manager.CreateEntity();
            entity_manager.AddComponentData(singleton_entity, new ConfigEntityTag());
            entity_manager.AddComponentData(singleton_entity, new BuffConfigRegistry
            {
                configs = config_blob_,
            });

            // 初始化 Manager
            BuffDotsManager.Instance.Initialize(default_world_);
            Debug.Log($"BuffDotsBootstrap: Initialized with {buff_configs_?.Length ?? 0} configs.");
        }

        private void CreateConfigBlobAsset()
        {
            if (buff_configs_ == null || buff_configs_.Length == 0)
            {
                Debug.LogWarning("No BuffConfigData provided. Creating empty config blob.");
            }

            using var blob_builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blob_builder.ConstructRoot<BuffConfigBlobArray>();

            int count = buff_configs_?.Length ?? 0;
            var array = blob_builder.Allocate(ref root.items, count);

            for (int i = 0; i < count; i++)
            {
                var config = buff_configs_[i];
                array[i] = new BuffConfigBlob
                {
                    config_id = config.config_id_,
                    buff_name = config.buff_name_,
                    effect_type = config.effect_type_,
                    tag_type = config.tag_type_,
                    duration = config.duration_,
                    max_count = config.max_count_,
                    trigger_interval = config.trigger_interval_,
                    is_refreshable = config.is_refreshable_,
                    is_dispellable = config.is_dispellable_,
                };
            }

            config_blob_ = blob_builder.CreateBlobAssetReference<BuffConfigBlobArray>(Allocator.Persistent);
        }

        private void DisposeBlobAssets()
        {
            if (config_blob_.IsCreated)
            {
                config_blob_.Dispose();
            }
        }
    }
}