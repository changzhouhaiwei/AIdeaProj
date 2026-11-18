using MemoryPack;
using MemoryPack.Formatters;
using MemoryPack.Internal;

namespace Example.Cfg
{
	[MemoryPackable(/*Could not decode attribute arguments.*/)]
	public class LevelMatchConfig : IMemoryPackable<LevelMatchConfig>, IMemoryPackFormatterRegister
	{
		[Preserve]
		private sealed class LevelMatchConfigFormatter : MemoryPackFormatter<LevelMatchConfig>
		{
			[Preserve]
			public override void Serialize(ref MemoryPackWriter writer, ref LevelMatchConfig value)
			{
				LevelMatchConfig.Serialize(ref writer, ref value);
			}

			[Preserve]
			public override void Deserialize(ref MemoryPackReader reader, ref LevelMatchConfig value)
			{
				LevelMatchConfig.Deserialize(ref reader, ref value);
			}
		}

		[MemoryPackOrder(0)]
		public string Id { get; private set; }

		[MemoryPackOrder(1)]
		public int GroupId { get; private set; }

		[MemoryPackOrder(2)]
		public int GroupPriority { get; private set; }

		[MemoryPackOrder(3)]
		public int Priority { get; private set; }

		[MemoryPackOrder(4)]
		public LevelMatchType Type { get; private set; }

		[MemoryPackOrder(5)]
		public int IntParam1 { get; private set; }

		[MemoryPackOrder(6)]
		public int IntParam2 { get; private set; }

		[MemoryPackOrder(7)]
		public string StringParam1 { get; private set; }

		[MemoryPackOrder(8)]
		public string StringParam2 { get; private set; }

		[MemoryPackOrder(9)]
		public float FloatParam1 { get; private set; }

		[MemoryPackOrder(10)]
		public LevelMatchTarget Target { get; private set; }

		[MemoryPackOrder(11)]
		public LevelMatchOperator Operator { get; private set; }

		[MemoryPackOrder(12)]
		public string StringValue1 { get; private set; }

		[MemoryPackOrder(13)]
		public string StringValue2 { get; private set; }

		static LevelMatchConfig()
		{
			RegisterFormatter();
		}

		[Preserve]
		public static void RegisterFormatter()
		{
			if (!MemoryPackFormatterProvider.IsRegistered<LevelMatchConfig>())
			{
				MemoryPackFormatterProvider.Register<LevelMatchConfig>((MemoryPackFormatter<LevelMatchConfig>)(object)new LevelMatchConfigFormatter());
			}
			if (!MemoryPackFormatterProvider.IsRegistered<LevelMatchConfig[]>())
			{
				MemoryPackFormatterProvider.Register<LevelMatchConfig[]>((MemoryPackFormatter<LevelMatchConfig[]>)(object)new ArrayFormatter<LevelMatchConfig>());
			}
			if (!MemoryPackFormatterProvider.IsRegistered<LevelMatchType>())
			{
				MemoryPackFormatterProvider.Register<LevelMatchType>((MemoryPackFormatter<LevelMatchType>)(object)new UnmanagedFormatter<LevelMatchType>());
			}
			if (!MemoryPackFormatterProvider.IsRegistered<LevelMatchTarget>())
			{
				MemoryPackFormatterProvider.Register<LevelMatchTarget>((MemoryPackFormatter<LevelMatchTarget>)(object)new UnmanagedFormatter<LevelMatchTarget>());
			}
			if (!MemoryPackFormatterProvider.IsRegistered<LevelMatchOperator>())
			{
				MemoryPackFormatterProvider.Register<LevelMatchOperator>((MemoryPackFormatter<LevelMatchOperator>)(object)new UnmanagedFormatter<LevelMatchOperator>());
			}
		}

		[Preserve]
		public static void Serialize(ref MemoryPackWriter writer, ref LevelMatchConfig? value)
		{
			if (value == null)
			{
				((MemoryPackWriter)(ref writer)).WriteNullObjectHeader();
				return;
			}
			((MemoryPackWriter)(ref writer)).WriteObjectHeader((byte)14);
			((MemoryPackWriter)(ref writer)).WriteString(value.Id);
			int groupId = value.GroupId;
			int groupPriority = value.GroupPriority;
			int priority = value.Priority;
			LevelMatchType type = value.Type;
			int intParam = value.IntParam1;
			int intParam2 = value.IntParam2;
			((MemoryPackWriter)(ref writer)).WriteUnmanaged<int, int, int, LevelMatchType, int, int>(ref groupId, ref groupPriority, ref priority, ref type, ref intParam, ref intParam2);
			((MemoryPackWriter)(ref writer)).WriteString(value.StringParam1);
			((MemoryPackWriter)(ref writer)).WriteString(value.StringParam2);
			float floatParam = value.FloatParam1;
			LevelMatchTarget target = value.Target;
			LevelMatchOperator @operator = value.Operator;
			((MemoryPackWriter)(ref writer)).WriteUnmanaged<float, LevelMatchTarget, LevelMatchOperator>(ref floatParam, ref target, ref @operator);
			((MemoryPackWriter)(ref writer)).WriteString(value.StringValue1);
			((MemoryPackWriter)(ref writer)).WriteString(value.StringValue2);
		}

		[Preserve]
		public static void Deserialize(ref MemoryPackReader reader, ref LevelMatchConfig? value)
		{
			byte b = default(byte);
			if (!((MemoryPackReader)(ref reader)).TryReadObjectHeader(ref b))
			{
				value = null;
				return;
			}
			int groupId = default(int);
			int groupPriority = default(int);
			int priority = default(int);
			LevelMatchType type = default(LevelMatchType);
			int intParam = default(int);
			int intParam2 = default(int);
			float floatParam = default(float);
			LevelMatchTarget target = default(LevelMatchTarget);
			LevelMatchOperator @operator = default(LevelMatchOperator);
			string id;
			string stringParam;
			string stringParam2;
			string stringValue;
			string stringValue2;
			if (b == 14)
			{
				if (value != null)
				{
					id = value.Id;
					groupId = value.GroupId;
					groupPriority = value.GroupPriority;
					priority = value.Priority;
					type = value.Type;
					intParam = value.IntParam1;
					intParam2 = value.IntParam2;
					stringParam = value.StringParam1;
					stringParam2 = value.StringParam2;
					floatParam = value.FloatParam1;
					target = value.Target;
					@operator = value.Operator;
					stringValue = value.StringValue1;
					stringValue2 = value.StringValue2;
					id = ((MemoryPackReader)(ref reader)).ReadString();
					((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref groupId);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref groupPriority);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref priority);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<LevelMatchType>(ref type);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref intParam);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref intParam2);
					stringParam = ((MemoryPackReader)(ref reader)).ReadString();
					stringParam2 = ((MemoryPackReader)(ref reader)).ReadString();
					((MemoryPackReader)(ref reader)).ReadUnmanaged<float>(ref floatParam);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<LevelMatchTarget>(ref target);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<LevelMatchOperator>(ref @operator);
					stringValue = ((MemoryPackReader)(ref reader)).ReadString();
					stringValue2 = ((MemoryPackReader)(ref reader)).ReadString();
					goto IL_02d7;
				}
				id = ((MemoryPackReader)(ref reader)).ReadString();
				((MemoryPackReader)(ref reader)).ReadUnmanaged<int, int, int, LevelMatchType, int, int>(ref groupId, ref groupPriority, ref priority, ref type, ref intParam, ref intParam2);
				stringParam = ((MemoryPackReader)(ref reader)).ReadString();
				stringParam2 = ((MemoryPackReader)(ref reader)).ReadString();
				((MemoryPackReader)(ref reader)).ReadUnmanaged<float, LevelMatchTarget, LevelMatchOperator>(ref floatParam, ref target, ref @operator);
				stringValue = ((MemoryPackReader)(ref reader)).ReadString();
				stringValue2 = ((MemoryPackReader)(ref reader)).ReadString();
			}
			else
			{
				if (b > 14)
				{
					MemoryPackSerializationException.ThrowInvalidPropertyCount(typeof(LevelMatchConfig), (byte)14, b);
					return;
				}
				if (value == null)
				{
					id = null;
					groupId = 0;
					groupPriority = 0;
					priority = 0;
					type = (LevelMatchType)0;
					intParam = 0;
					intParam2 = 0;
					stringParam = null;
					stringParam2 = null;
					floatParam = 0f;
					target = LevelMatchTarget.None;
					@operator = LevelMatchOperator.None;
					stringValue = null;
					stringValue2 = null;
				}
				else
				{
					id = value.Id;
					groupId = value.GroupId;
					groupPriority = value.GroupPriority;
					priority = value.Priority;
					type = value.Type;
					intParam = value.IntParam1;
					intParam2 = value.IntParam2;
					stringParam = value.StringParam1;
					stringParam2 = value.StringParam2;
					floatParam = value.FloatParam1;
					target = value.Target;
					@operator = value.Operator;
					stringValue = value.StringValue1;
					stringValue2 = value.StringValue2;
				}
				if (b != 0)
				{
					id = ((MemoryPackReader)(ref reader)).ReadString();
					if (b != 1)
					{
						((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref groupId);
						if (b != 2)
						{
							((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref groupPriority);
							if (b != 3)
							{
								((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref priority);
								if (b != 4)
								{
									((MemoryPackReader)(ref reader)).ReadUnmanaged<LevelMatchType>(ref type);
									if (b != 5)
									{
										((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref intParam);
										if (b != 6)
										{
											((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref intParam2);
											if (b != 7)
											{
												stringParam = ((MemoryPackReader)(ref reader)).ReadString();
												if (b != 8)
												{
													stringParam2 = ((MemoryPackReader)(ref reader)).ReadString();
													if (b != 9)
													{
														((MemoryPackReader)(ref reader)).ReadUnmanaged<float>(ref floatParam);
														if (b != 10)
														{
															((MemoryPackReader)(ref reader)).ReadUnmanaged<LevelMatchTarget>(ref target);
															if (b != 11)
															{
																((MemoryPackReader)(ref reader)).ReadUnmanaged<LevelMatchOperator>(ref @operator);
																if (b != 12)
																{
																	stringValue = ((MemoryPackReader)(ref reader)).ReadString();
																	if (b != 13)
																	{
																		stringValue2 = ((MemoryPackReader)(ref reader)).ReadString();
																		_ = 14;
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				if (value != null)
				{
					goto IL_02d7;
				}
			}
			value = new LevelMatchConfig
			{
				Id = id,
				GroupId = groupId,
				GroupPriority = groupPriority,
				Priority = priority,
				Type = type,
				IntParam1 = intParam,
				IntParam2 = intParam2,
				StringParam1 = stringParam,
				StringParam2 = stringParam2,
				FloatParam1 = floatParam,
				Target = target,
				Operator = @operator,
				StringValue1 = stringValue,
				StringValue2 = stringValue2
			};
			return;
			IL_02d7:
			value.Id = id;
			value.GroupId = groupId;
			value.GroupPriority = groupPriority;
			value.Priority = priority;
			value.Type = type;
			value.IntParam1 = intParam;
			value.IntParam2 = intParam2;
			value.StringParam1 = stringParam;
			value.StringParam2 = stringParam2;
			value.FloatParam1 = floatParam;
			value.Target = target;
			value.Operator = @operator;
			value.StringValue1 = stringValue;
			value.StringValue2 = stringValue2;
		}
	}
}

