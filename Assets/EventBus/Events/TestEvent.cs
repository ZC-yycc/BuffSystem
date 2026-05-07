using UnityEngine;

public struct TestEvent : IEventBus {}

public struct TestEvent_i : IEventBus<int> {}

public struct TestEvent_ii : IEventBus<int, int> {}

public struct TestEvent_iii : IEventBus<int, int, int> {}
