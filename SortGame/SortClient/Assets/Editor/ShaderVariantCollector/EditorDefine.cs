using System;

namespace YooAsset.Editor
{

	/// <summary>
	/// 资源包流水线的构建模式
	/// </summary>
	public enum EBuildMode
	{
		/// <summary>
		/// 强制重建模式
		/// </summary>
		ForceRebuild,

		/// <summary>
		/// 增量构建模式
		/// </summary>
		IncrementalBuild,

		/// <summary>
		/// 演练构建模式
		/// </summary>
		DryRunBuild,

		/// <summary>
		/// 模拟构建模式
		/// </summary>
		SimulateBuild,
	}
	/// <summary>
	/// 资源搜索类型
	/// </summary>
	public enum EAssetSearchType
	{
		All,
		RuntimeAnimatorController,
		AnimationClip,
		AudioClip,
		AudioMixer,
		Font,
		Material,
		Mesh,
		Model,
		PhysicMaterial,
		Prefab,
		Scene,
		Script,
		Shader,
		Sprite,
		Texture,
		VideoClip,
	}

	/// <summary>
	/// 资源文件格式
	/// </summary>
	public enum EAssetFileExtension
	{
		prefab,
		unity,
		fbx,
		anim,
		controller,
		png,
		jpg,
		mat,
		shader,
		ttf,
		cs,
	}
}