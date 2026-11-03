---
name: smart-ambience
description: Ambient context and distributed dataflow with SmartAmbience (.NET) - AmbientField, context adapters, AmbienceHub snapshots/restore, ambient endpoints and FlowingContracts. Use when infrastructure context (tenant, scope, correlation id, routing info etc.) must flow through layers, async/thread switches or across service boundaries without polluting business signatures.
---

# SmartAmbience — Ambient Context & Distributed Dataflow Skill

**Version:** 1.0  
**Status:** Desired State / Normative Skill Reference  
**Primary reference implementation:** C# / .NET  
**Primary audience:** AI agents and developers designing, implementing, reviewing, integrating, or extending SmartAmbience-based context and dataflow infrastructure  
**Maintenance model:** Incremental, Git-managed; preserve this structure after V1 and keep future diffs small and localized

---

## 1. Purpose and Required Agent Capabilities

SmartAmbience provides a compact infrastructure model for **ambient context**, **context survival**, **context isolation**, and **transport-agnostic snapshot/restore dataflow**.

The framework exists because context-like infrastructure data is often needed by many layers of an application while it should **not** become part of every business method signature.

Typical examples include:

- tenant or profile binding identifiers;
- logical scope identifiers;
- system-wide call-tree or correlation identifiers;
- infrastructure-zone or locality information;
- routing/discovery context;
- transaction-like ambient identifiers;
- diagnostic or operational call context;
- other infrastructure state that belongs to a logical execution rather than to the business contract.

The central design goal is:

> **Infrastructure context should be available wherever the logical operation needs it, should survive relevant execution-context transitions, should remain strictly isolated from unrelated operations, and should be able to flow across process/service boundaries without polluting business signatures.**

This document is intentionally more than an API summary.

An AI agent using this skill MUST be able to:

1. understand the SmartAmbience mental model and distinguish it from plain `AsyncLocal<T>`;
2. use `AmbientField` correctly in .NET code;
3. design clean state-manager/facade APIs that encapsulate raw `AmbientField` instances;
4. understand and configure the technology-specific context-adapter layer;
5. evaluate whether a hosting/runtime technology can provide a safe fallback context;
6. implement a new `IAmbienceToSomeContextAdapter` when required;
7. reason about lifetime, parallelism, request isolation, thread switching, async switching, disposal, and context termination;
8. use `AmbienceHub` to capture and restore contextual snapshots;
9. implement and use custom ambient endpoints;
10. understand optional FlowingContracts and use them only when the additional selection complexity is justified;
11. integrate SmartAmbience with a transport without coupling SmartAmbience to that transport;
12. understand UJMW as a concrete compatible sidechannel transport example;
13. apply SmartAmbience to multi-hop tenancy and other distributed-context scenarios;
14. use the ambient scope as an external partitioning/binding input for caches, scoped singleton helpers, or DI-container instance management;
15. preserve the strict separation between **context propagation** and **authentication/authorization**;
16. use snapshot/restore outside remote calls, e.g. to freeze contextual application state when scheduling work and restore it later;
17. identify unsafe or misleading designs, especially those that can accidentally mix two logical call contexts.

### 1.1 Desired-state emphasis

This document primarily defines **what a good SmartAmbience architecture looks like**.

Migration and adoption guidance is secondary and appears near the end.

Existing implementation details are evidence for the desired state, but an AI agent MUST distinguish:

- deliberate architectural concepts;
- public capabilities;
- advanced/special-purpose capabilities;
- implementation details;
- historical or compatibility behavior.

### 1.2 Normative terminology

The following terms are used throughout this document:

| Term | Meaning |
|---|---|
| **MUST** | Mandatory rule. Deviation requires an explicit architectural reason. |
| **MUST NOT** | Prohibited by default. |
| **SHOULD** | Strong recommendation. Deviation should have a concrete technical reason. |
| **SHOULD NOT** | Avoid unless justified by the concrete environment. |
| **MAY** | Permitted capability, not a default requirement. |

When rules conflict, prioritize:

1. context isolation and correctness;
2. security boundary correctness;
3. preservation of logical scope;
4. deterministic behavior;
5. explicit project-specific requirements;
6. performance;
7. convenience.

---

## 2. Executive Mental Model

SmartAmbience can be understood as two tightly related layers.

```text
LOCAL EXECUTION / HOST LIFETIME
────────────────────────────────────────────────────────────────────

Business / Infrastructure Code
        │
        ▼
   AmbientField
        │
        ├── fast/local ambient value
        │     based on AsyncLocal-style flow
        │
        └── technology-specific fallback/root context
              via IAmbienceToSomeContextAdapter
                        │
                        ▼
          robust logical context preservation
          WITHOUT mixing unrelated calls


DISTRIBUTED / DEFERRED DATAFLOW
────────────────────────────────────────────────────────────────────

current ambient state
        │
        ▼
    AmbienceHub
        │
        ├── exposed AmbientFields
        └── custom ambient endpoints
        │
        ▼
   capture snapshot
        │
        ▼
 transport / persist / schedule / queue
        │
        ▼
   restore snapshot
        │
        ▼
 reconstructed logical context
```

The two layers solve different problems:

- **Context survival** answers:  
  “How does a logical value remain available inside one runtime/host even if the expected asynchronous execution context is lost or changed?”

- **Dataflow** answers:  
  “How does the relevant logical context cross a process, service, machine, scheduling, queueing, or temporal boundary?”

These mechanisms are related but MUST NOT be conflated.

A context switch inside one service is not the same problem as a service hop.

A service hop is not merely a larger `AsyncLocal`.

### 2.1 The defining architectural idea

SmartAmbience treats ambient infrastructure state as a first-class logical context that is:

- locally accessible without being threaded through every method parameter;
- resilient against relevant context loss where a suitable host-specific root exists;
- isolated between unrelated calls;
- capturable as a string-key/string-value snapshot;
- restorable in another execution environment;
- extensible beyond individual scalar fields;
- selectable through optional flowing contracts;
- transport-agnostic.

### 2.2 Why this matters

Without such a layer, an application frequently ends up with one or more of the following problems:

- tenant identifiers duplicated in nearly every business method;
- infrastructure context coupled to controller signatures;
- service boundaries dictating tenancy architecture;
- tracing/correlation logic duplicated per transport;
- global caches that must manually include tenant identifiers in every key;
- request-local caches that are too short-lived and lose valuable reuse;
- service-discovery decisions coupled directly to business code;
- context lost during async/thread transitions;
- context propagated across services through ad-hoc headers with inconsistent semantics;
- authorization logic unable to evaluate tenant-specific scopes because tenant context is unavailable at the correct point.

SmartAmbience intentionally provides a common low-level mechanism beneath these concerns.

---

## 3. Architectural Positioning

### 3.1 SmartAmbience is not merely `AsyncLocal<T>`

`AsyncLocal<T>` is a useful .NET primitive for data local to an asynchronous control flow.

SmartAmbience builds a stronger model around this idea.

A plain `AsyncLocal<T>` can work very well when the runtime preserves the `ExecutionContext` exactly as expected.

Enterprise hosting environments can be more complicated:

- a request pipeline may execute portions of one logical request on different threads;
- framework code may suppress, replace, detach, or otherwise alter execution-context flow;
- callbacks may execute outside the `AsyncLocal` lineage expected by application code;
- a host-specific request/operation context may remain available even when the ambient async value is no longer available;
- an object representing a context may still exist while already being disposed or unusable;
- multiple requests can execute concurrently and MUST NOT see one another's state.

SmartAmbience therefore combines an `AsyncLocal`-style inner value with a **technology-specific fallback context**.

The goal is not “never lose a value at any cost”.

The goal is:

> **Preserve a value as robustly as the hosting technology safely permits, but never preserve it by merging unrelated logical contexts.**

### 3.2 SmartAmbience is not a global context dictionary

A process-global dictionary would appear robust because values do not disappear during thread switches.

It is also fundamentally unsafe for call-specific state.

If two requests execute concurrently, a global context store can cause:

- tenant A to observe tenant B's identifier;
- one call to inherit another call's correlation identifier;
- cache selection to cross tenant boundaries;
- infrastructure routing to use another request's origin context;
- authorization checks to evaluate the wrong logical scope.

Therefore:

> **Availability without isolation is not correctness.**

### 3.3 SmartAmbience is not a security mechanism

SmartAmbience carries context.

It does **not** authenticate that context and does **not** authorize access based on that context.

A restored tenant identifier says:

> “This is the tenant context that was transported/restored.”

It does **not** say:

> “The caller is authorized to act as this tenant.”

Security MUST be established by an appropriate authentication/authorization layer.

### 3.4 SmartAmbience is not tied to web services

Remote service calls are a major use case, but Capture/Restore is more general.

A snapshot can be:

- placed into a web-service sidechannel;
- written into a queue message;
- persisted with a scheduled job;
- attached to a background-work item;
- stored with a mass-processing plan;
- passed through a custom IPC mechanism;
- retained as part of an application-state checkpoint.

The later execution can restore the snapshot and continue inside the reconstructed logical context.

### 3.5 SmartAmbience is not a DI container

SmartAmbience does not itself have to manage object lifetimes.

However, an ambient binding/scope identifier can be used as a **partition key** by:

- a scoped-singleton helper;
- a cache manager;
- a dependency-injection container customization;
- another instance-management layer.

This distinction is important.

SmartAmbience provides the logical scope signal.

Another component can use that signal to choose or create the correct instance.

---

## 4. Core Concepts and Vocabulary

### 4.1 Ambient state

**Ambient state** is contextual infrastructure data that is logically associated with the current execution but does not belong in every explicit business signature.

Examples:

```text
TenantId
ProfileBindingIdentifier
LogicalScopeId
CallTreeGuid
InfrastructureZone
CorrelationId
TransactionHandle
```

The concrete names above are examples, not mandatory framework-defined fields.

### 4.2 Logical context

A **logical context** is the conceptual execution scope to which ambient values belong.

It is not necessarily identical to:

- one OS thread;
- one `Task`;
- one HTTP request object;
- one ASP.NET DI request scope;
- one process;
- one service invocation;
- one physical connection.

The host-specific adapter exists precisely because the best technical carrier for a logical context differs by technology.

### 4.3 Context adapter

An `IAmbienceToSomeContextAdapter` bridges SmartAmbience to a hosting/runtime-specific context that can act as a fallback/root for ambient values.

The adapter exposes:

```csharp
bool IsUsable { get; }

void SetCurrentValue(string key, string value);

string TryGetCurrentValue(string key);

event CurrentContextIsTerminatingEventHandler CurrentContextIsTerminating;
```

Conceptually the adapter answers:

- Is the host context currently safe to use?
- Store this ambient value in the current host context.
- Retrieve this ambient value from the current host context.
- Tell SmartAmbience when this host context is about to terminate.

### 4.4 Exposed AmbientField

An `AmbientField` can be marked as exposed.

Exposed fields participate in the AmbienceHub dataflow model.

Their names become stable keys for snapshot/restore.

### 4.5 AmbienceHub

`AmbienceHub` is the central dataflow orchestrator.

It can:

- capture the current ambient state;
- restore a previously captured ambient state;
- include exposed `AmbientField` instances;
- include custom endpoints;
- optionally apply a FlowingContract;
- enforce endpoint naming/prefix separation;
- restore endpoints in a dependency-aware order;
- provide diagnostic dump output.

### 4.6 Custom endpoint

A custom endpoint extends the ambient snapshot beyond individual `AmbientField` instances.

It provides two operations:

```text
capture current endpoint state
restore incoming endpoint state
```

Use cases include state that is naturally represented as a set of values rather than one scalar ambient field.

### 4.7 FlowingContract

A FlowingContract optionally selects which ambient data is allowed to participate in a particular capture/restore operation.

It can include/exclude:

- exposed AmbientFields;
- custom endpoints.

FlowingContracts are an **optional advanced feature**, not a prerequisite for SmartAmbience dataflow.

### 4.8 Snapshot

A snapshot is conceptually:

```csharp
IEnumerable<KeyValuePair<string, string>>
```

or a compatible dictionary-like representation.

This deliberately small representation is one reason SmartAmbience can remain transport-agnostic.

---

## 5. `AmbientField` — Desired-State Semantics

### 5.1 Purpose

An `AmbientField` stores one named string value associated with the current logical context.

Internally, the reference implementation combines:

- an `AsyncLocal<string>`-based local value;
- a host-specific external context adapter;
- a long-living fallback for situations where the external context is not usable.

The intended behavior is more important than the exact implementation structure.

### 5.2 Read priority and self-healing

The reference implementation follows the conceptual read sequence:

```text
1. ensure a context adapter is configured
2. inspect the inner AsyncLocal value

   if present:
       use it

   else if host context is usable:
       read the value from the host context
       repopulate the AsyncLocal value
       return the recovered value

   else:
       fall back to the long-living value
       repopulate the AsyncLocal value
       return it
```

The important concept is the **self-healing local context**:

> If the inner async context is lost but the technology-specific root context still contains the correct value, SmartAmbience recovers the value and writes it back into the local ambient carrier.

This supports subsequent child execution paths again.

### 5.3 Write semantics

When setting a value:

- if the context adapter is usable, the value is stored in that host context;
- otherwise, the long-living fallback is used;
- the inner async-local value is updated as well.

An agent MUST understand that the host adapter is therefore not merely a reader used after failure.

It participates in normal context persistence when usable.

### 5.4 Explicit `null`

The reference implementation distinguishes:

- “no value exists / context value was lost”
- “the intentional value is `null`”

Since `null` can indicate missing `AsyncLocal` state and some fallback stores may not preserve `null` distinctly, SmartAmbience internally uses a sentinel representation for explicitly stored null.

This is an internal wire/storage concern.

Consumer code should reason in normal semantic values.

### 5.5 Context adapter requirement

Before normal `AmbientField` use, a context adapter must be configured.

This is deliberate.

An application should not silently operate without an understood lifetime/root-context model.

### 5.6 Long-living fallback guard

`AssertLongLivingValueIsNotUsed()` exists for code paths that expect to execute inside a real request/operation lifetime and want to fail rather than silently use the long-living fallback.

This is important when incorrect lifetime would be more dangerous than an exception.

Example reasoning:

```text
This method MUST execute under an isolated request context.
A process-level fallback would make isolation ambiguous.
Therefore assert that the context adapter is usable first.
```

### 5.7 Sealing

`SealContextValue()` can mark a context value as sealed so that it cannot later be changed to a different value in that context.

This is an advanced/special-purpose capability.

It is potentially useful in restrictive environments where a binding identifier must become immutable after initialization.

It is **not** the default pattern that every AmbientField should use.

### 5.8 Temporary branch override

`InvokeUnderTemporaryBranch(...)` temporarily overrides the inner ambient value while executing a delegate and restores the previous local value afterward.

This is an advanced branching tool.

An agent MUST understand that temporary ambient overrides can be subtle.

Use them only when the intended branch semantics are clear.

### 5.9 Diagnostics

`AmbientField.DebugInfo` captures diagnostic information about recent get behavior, including whether:

- the host context was usable;
- a host-context value existed;
- an async-local value existed;
- a long-living value participated.

These diagnostics are useful when investigating context-loss behavior.

They are not business semantics.

---

## 6. Encapsulation Pattern for Application Ambient State

### 6.1 Do not expose raw AmbientField objects as the normal application API

The preferred application pattern is:

- keep the `AmbientField` itself private;
- expose a deliberately named property representing the semantic value.

Conceptual example:

```csharp
public static class TenantState {

  private static readonly AmbientField _CurrentTenantField =
    new AmbientField("CurrentTenant", true);

  public static string CurrentTenant {
    get {
      return _CurrentTenantField.Value;
    }
    set {
      _CurrentTenantField.Value = value;
    }
  }

}
```

The exact class/property names depend on the project.

The architectural point is stable:

> **Application code should depend on semantic context properties, not on raw ambient-storage primitives.**

### 6.2 Why encapsulation matters

Encapsulation allows the application layer to evolve independently of SmartAmbience internals.

The facade can later add:

- validation;
- conversion;
- defaults;
- lifecycle assertions;
- sealing;
- structured identifiers;
- logging;
- compatibility behavior.

It also keeps implementation details such as `AmbientField.Value` out of unrelated business code.

### 6.3 Exposed names

For an exposed field, its name is observable in flowing scenarios.

The name should therefore be:

- stable;
- meaningful;
- sufficiently unique within the ambient data domain.

Do not invent unnecessarily elaborate global naming schemes.

The number of binding-identifier-style values is normally limited, and custom endpoints provide their own namespace prefixing.

---

## 7. Context-Switch Robustness

### 7.1 The central challenge

The difficult part of ambient context is not storing a value.

The difficult part is knowing **what technical object still represents the same logical call after execution moved**.

Different .NET hosting technologies can have very different behavior.

Examples of possible execution patterns include:

- a logical request continuing on another thread;
- several internal asynchronous operations executing concurrently;
- a host request context surviving while `AsyncLocal` lineage is interrupted;
- callbacks executing after a framework scheduling boundary;
- an operation context becoming unusable before all references disappear;
- background execution intentionally leaving the request context.

A universal storage location cannot safely model all of these.

### 7.2 The two requirements that must both hold

A good adapter must satisfy both:

```text
ROBUSTNESS:
  recover the logical context whenever the hosting technology
  still provides a reliable identity/root for that same call

AND

ISOLATION:
  never make context from one logical call visible to another
```

Optimizing only one side is incorrect.

### 7.3 Why technology adapters exist

A context adapter is the intentional place where hosting-specific knowledge belongs.

It knows:

- which host object identifies the current request/operation;
- how values can be attached to it;
- when the object can no longer be used;
- how concurrent requests are isolated;
- when termination occurs.

SmartAmbience itself does not need to hardcode these host differences.

### 7.4 The “rescue anchor” model

A useful mental model is:

```text
AsyncLocal path lost?
        │
        ▼
Does the hosting technology still expose a context object
that is guaranteed to represent the SAME logical call?
        │
       yes
        │
        ▼
recover from that context object
and repopulate the local ambient value
```

The rescue anchor MUST be:

- stable enough to survive the problematic switch;
- scoped tightly enough not to merge calls.

### 7.5 A too-wide context is worse than no fallback

Suppose a host does not expose a safe per-call context.

Using an application-global dictionary “to make it work” is usually wrong.

For tenant-aware data:

```text
missing tenant context
```

is an obvious failure.

But:

```text
wrong tenant context from another concurrent call
```

is a silent isolation violation.

Therefore an adapter MUST prefer explicit unavailability over unsafe sharing.

---

## 8. Implementing `IAmbienceToSomeContextAdapter`

An AI agent is expected to be able to implement new context adapters.

This is an advanced capability and requires runtime analysis before code generation.

### 8.1 Investigation checklist

Before implementing an adapter, determine:

| Question | Why it matters |
|---|---|
| What object represents one logical request/operation? | Defines isolation boundary |
| Is that object available across thread switches? | Determines rescue capability |
| Is it available across async continuations? | Determines normal flow |
| Can several requests run concurrently? | Tests cross-call safety |
| Can the context be reused by the host? | May create stale-value leakage |
| Where can arbitrary key/value data be attached? | Defines storage |
| Does storage permit `null`? | Affects representation |
| When is the context disposed/terminated? | Defines usability and cleanup |
| Can property access throw after disposal? | Must be handled |
| Is there a reliable termination event/callback? | Needed for lifecycle notification |
| Can work legally continue after request termination? | Determines fallback semantics |
| Is the context inherited by child execution? | Determines interaction with AsyncLocal |
| Can host code suppress logical context flow? | Determines failure modes |
| Are nested or re-entrant operations possible? | May require stronger isolation reasoning |

Do not implement the final adapter until these questions are sufficiently understood.

### 8.2 `IsUsable`

`IsUsable` MUST mean:

> The current host context can safely be used now for `SetCurrentValue` and `TryGetCurrentValue`.

It should not merely mean:

> A context reference is non-null.

A host context can exist but already be disposed.

### 8.3 `SetCurrentValue`

`SetCurrentValue` must store the given key/value in the **current logical host context**.

It MUST NOT:

- silently redirect to a process-global dictionary for request-scoped data;
- use another request's context;
- create an independent scope that cannot be recovered consistently.

### 8.4 `TryGetCurrentValue`

`TryGetCurrentValue` returns the value associated with the key in the current host context, or `null` when no value is present.

It must remain consistent with the storage selected by `SetCurrentValue`.

### 8.5 Termination event

`CurrentContextIsTerminating` signals that the current technology-specific ambience container is about to terminate.

This enables SmartAmbience consumers to react while the dying context can still be inspected.

An adapter should raise this at the narrowest reliable lifecycle boundary provided by the host.

### 8.6 Disposal races

If a host context can become disposed between checks and property access, the adapter must handle that race safely.

The ASP.NET Core reference adapter demonstrates this pattern:

- retrieve the current `HttpContext`;
- try to access features/items;
- catch `ObjectDisposedException`;
- clear the stale current context;
- report the adapter as unusable / return no value.

The broader lesson is:

> **Adapter usability must reflect the host's real lifecycle, not an optimistic null check.**

### 8.7 Context storage must be per-call

If the hosting technology offers `Items`, `CallContext`, `OperationContext`, request features, or another request-scoped property bag, that can be an appropriate backing store.

The name of the API is unimportant.

The key requirement is that the storage lifecycle maps to the desired logical call boundary.

### 8.8 Adapter validation tests

A new adapter SHOULD be tested at least for:

1. simple synchronous get/set;
2. normal async continuation;
3. thread switch inside the same logical request;
4. parallel independent requests;
5. nested child work;
6. request/context disposal;
7. access after termination;
8. intentional absence of host context;
9. repeated requests on pooled host threads;
10. stress/concurrency where two tenant values are continuously alternated in parallel.

The most important test invariant is:

```text
No execution belonging to logical context A
may ever observe a value belonging to logical context B.
```

### 8.9 When no safe adapter exists

If the host provides no safe context root that survives the problematic transitions:

- do not fake one through global mutable state;
- document the limitation;
- rely on supported local context flow where possible;
- consider explicit snapshot/restore around the problematic scheduling boundary;
- redesign the boundary if required.

---

## 9. ASP.NET Core Reference Adapter

The supplied ASP.NET Core integration is a useful reference implementation.

It is not the only valid adapter model.

### 9.1 Model

The middleware keeps the current `HttpContext` in an adapter-local `AsyncLocal<HttpContext>` and uses:

```text
HttpContext.Items
```

as the host-context key/value store.

Conceptually:

```text
request enters middleware
        │
        ▼
adapter.CurrentHttpContext = context
        │
        ▼
inner pipeline executes
        │
        ▼
termination event raised
        │
        ▼
adapter.CurrentHttpContext = null
```

### 9.2 Usability

The adapter checks whether:

- a current `HttpContext` exists;
- its features can still be accessed.

It also handles `ObjectDisposedException`.

### 9.3 Setup

The framework supplies an application-builder extension that:

1. registers the adapter middleware;
2. assigns the adapter to `AmbientField.ContextAdapter`.

The architectural rule is more general than the exact extension method:

> The correct host adapter must be initialized before ambient values are used in that host.

---

## 10. AmbienceHub — Snapshot and Restore

### 10.1 Purpose

`AmbienceHub` is the central component for converting the current ambience into a transportable/restorable snapshot.

It deliberately operates on simple string key/value data.

### 10.2 Capture

The hub can capture into:

```csharp
IDictionary<string, string>
```

or through a callback:

```csharp
Action<string, string>
```

Conceptual example:

```csharp
Dictionary<string, string> snapshot =
  new Dictionary<string, string>();

AmbienceHub.CaptureCurrentValuesTo(snapshot);
```

The snapshot is now independent of the local `AsyncLocal`/host-context implementation.

### 10.3 Restore

A snapshot can later be restored:

```csharp
AmbienceHub.RestoreValuesFrom(snapshot);
```

Conceptually:

```text
serialized / transported / persisted state
        │
        ▼
AmbienceHub.RestoreValuesFrom(...)
        │
        ├── restore exposed AmbientFields
        └── restore custom endpoints
```

### 10.4 Empty versus null restore input

The implementation distinguishes:

- `null` source;
- an empty source.

This is semantically meaningful.

A null source can invoke the configurable `OnRestoreNullHandler`.

The default behavior is to fail unless null-restore semantics have been deliberately configured.

An empty collection, by contrast, is an actual restore operation with zero incoming entries.

An agent MUST NOT casually normalize these two states into one.

### 10.5 Diagnostic dump

`CaptureCurrentValuesAsDump(...)` produces a human-readable representation of current ambient data.

This is useful for:

- troubleshooting;
- logging diagnostics;
- integration testing;
- validating FlowingContract behavior.

It should not be treated as a stable machine wire format.

---

## 11. Exposed AmbientFields and Pre-Staging

### 11.1 Why exposed fields exist

Only fields marked for exposure need to participate in generic dataflow.

A private local ambient implementation detail should not automatically become transportable.

### 11.2 Pre-staging problem

A distributed snapshot can arrive **before the owner of a particular AmbientField has initialized that field instance**.

This can happen with lazy state-manager initialization.

Without special handling:

```text
incoming snapshot contains TenantId
        │
        ▼
TenantState class not initialized yet
        │
        ▼
AmbientField instance does not yet exist
        │
        X  value would be lost
```

### 11.3 Pre-staging solution

During restore, if no matching exposed field instance exists yet, SmartAmbience can inject the value into the current context adapter under the future field name.

Later, when the exposed `AmbientField` instance is created, its constructor checks the adapter for such a staged value and synchronizes it into the local async state.

Conceptually:

```text
restore
  └── field not constructed
        └── pre-stage in context adapter

later:
field constructor
  └── discover pre-staged value
        └── hydrate inner ambient value
```

This capability is essential for robust lazy initialization.

### 11.4 Agent rule

An agent MUST NOT assume all exposed fields are constructed before a snapshot is restored.

When implementing compatible behavior, preserve pre-staging semantics.

---

## 12. Custom Ambient Endpoints

### 12.1 Purpose

Not all ambient state is best represented as one `AmbientField` per value.

A custom endpoint can expose a complete contextual subsystem to the hub.

Conceptually:

```csharp
AmbienceHub.BindCustomEndpoint(
  "DiscoveryContext",
  captureMethod,
  restoreMethod
);
```

The concrete capture/restore methods can map arbitrary subsystem state to/from string key/value pairs.

### 12.2 Appropriate use cases

Custom endpoints are appropriate for:

- a collection of related contextual values;
- existing legacy ambience subsystems;
- dynamic context dimensions;
- discovery/routing state;
- call-tree metadata;
- infrastructure state that already has its own capture/restore abstraction.

### 12.3 Endpoint namespace

Normal custom endpoints receive a hub-managed key prefix based on the endpoint name.

Conceptually:

```text
endpoint name:
  DiscoveryContext

endpoint-local key:
  OriginZone

snapshot key:
  DiscoveryContext.OriginZone
```

This keeps independently developed endpoints from colliding.

### 12.4 Collision protection

An endpoint must not capture duplicate keys into the same snapshot.

The hub treats duplicate captured keys as an error.

This is important because a snapshot must have unambiguous restore semantics.

### 12.5 Restore ordering

Some contextual state depends on another state already being restored.

A custom endpoint can declare restore dependencies through `restoreAfter`.

Conceptually:

```text
Binding identifiers restored
        │
        ▼
Discovery context restored
        │
        ▼
Dependent component state restored
```

An agent developing endpoints MUST identify such dependencies explicitly rather than relying on registration accident.

### 12.6 Restore performance

Endpoint registration can define an expected maximum restore duration.

The hub can invoke a warning hook when an endpoint exceeds this threshold.

Ambient restoration often occurs directly in the critical path before business execution.

Therefore custom restore handlers SHOULD be:

- deterministic;
- bounded;
- fast;
- free of unnecessary remote calls;
- explicit about expensive behavior.

### 12.7 Capture/restore symmetry

A custom endpoint SHOULD satisfy:

```text
state S
   ↓ capture
snapshot E
   ↓ restore into equivalent fresh context
state S'
```

where `S'` is semantically equivalent to `S` for the contextual behavior that endpoint owns.

Exact object identity is not required.

Semantic reconstruction is.

---

## 13. FlowingContracts — Optional Selection Layer

### 13.1 Purpose

A FlowingContract defines a named subset of ambient state.

It can include:

- all exposed AmbientFields;
- selected exposed AmbientFields;
- all custom endpoints;
- selected custom endpoints;
- exclusions.

### 13.2 Optional by design

FlowingContracts add configuration and conceptual complexity.

They SHOULD NOT be introduced merely because the capability exists.

SmartAmbience can operate without configured FlowingContracts.

When no contracts are configured, the built-in default definition can allow the normal complete ambient capture/restore behavior.

### 13.3 Once FlowingContracts are enabled

When explicit FlowingContracts are configured, capture/restore calls must identify the intended contract.

This prevents ambiguous accidental selection.

### 13.4 Simple default pattern

A project that wants explicit configuration but only one practical flowing set may define one contract, commonly a project-defined `"default"` contract, including all relevant exposed fields and custom endpoints.

Conceptually:

```csharp
AmbienceHub.DefineFlowingContract(
  "default",
  (FlowingContractDefinition flow) => {
    flow.IncludeAllExposedAmbientFieldInstances();
    flow.IncludeAllCustomEndpoints();
  }
);
```

This is a valid pattern.

It does not mean every application needs a FlowingContract.

### 13.5 Multiple contracts

Multiple contracts become useful when different boundaries intentionally require different context sets.

Examples:

```text
internal-service-flow
external-partner-flow
background-job-flow
diagnostics-only-flow
```

The concrete contract names are project-specific.

### 13.6 Immutability

A FlowingContract definition becomes immutable after setup.

This avoids runtime mutation of a contract whose meaning should be stable.

### 13.7 Security relevance without being security itself

A smaller FlowingContract can reduce accidental data exposure across a transport boundary.

However:

> **FlowingContract selection is not authorization.**

It determines what is captured/restored.

It does not prove the caller may use those values.

---

## 14. Transport-Agnostic Dataflow

### 14.1 Core invariant

SmartAmbience does not need to know HTTP, RPC, message queues, files, or schedulers.

A compatible transport/persistence boundary only needs to:

1. obtain a snapshot from `AmbienceHub`;
2. encode/store/transmit the key/value pairs;
3. reconstruct the key/value pairs at the destination;
4. pass them to `AmbienceHub.RestoreValuesFrom(...)`.

Conceptually:

```text
SmartAmbience
  CaptureCurrentValuesTo(...)
        │
        ▼
      snapshot
        │
        ▼
  arbitrary carrier
        │
        ▼
      snapshot
        │
        ▼
SmartAmbience
  RestoreValuesFrom(...)
```

### 14.2 Carrier requirements

A carrier must preserve:

- keys;
- values;
- null/absence semantics as required by the integration;
- the selected snapshot as one logical unit.

A carrier should not reinterpret the meaning of ambient entries.

### 14.3 Business signature independence

Ambient data SHOULD NOT be added as ordinary business parameters merely because a transport boundary exists.

Wrong conceptual direction:

```csharp
Invoice LoadInvoice(
  long invoiceId,
  string tenantId,
  string callTreeGuid,
  string infrastructureZone
);
```

when those extra arguments are purely infrastructure context.

Preferred conceptual direction:

```csharp
Invoice LoadInvoice(long invoiceId);
```

with infrastructure context transported separately.

This keeps service contracts stable as infrastructure evolves.

---

## 15. UJMW Integration as a Concrete Transport Example

### 15.1 Relationship

UJMW and SmartAmbience are separate frameworks.

Their responsibilities are:

```text
SmartAmbience
  └── defines ambient-state capture/restore semantics

UJMW
  └── provides an RPC transport and configurable sidechannels
```

They were designed to compose cleanly.

### 15.2 UJMW `_` sidechannel

UJMW can carry a string dictionary in the reserved `_` property of its request/response wrapper DTO.

Conceptual request:

```json
{
  "_": {
    "CurrentTenant": "tenant-17",
    "CallTreeGuid": "f1c8...",
    "InfrastructureZone": "zone-a"
  },
  "invoiceId": 42
}
```

The keys inside `_` are infrastructure sidechannel data.

They are not business method arguments.

### 15.3 Conceptual client bridge

The client-side integration is conceptually:

```csharp
sideChannelConfiguration.ProvideUjmwUnderlineProperty();

sideChannelConfiguration.CaptureDataVia(
  (IDictionary<string, string> container) =>
    AmbienceHub.CaptureCurrentValuesTo(container)
);
```

When a FlowingContract is used:

```csharp
sideChannelConfiguration.CaptureDataVia(
  (IDictionary<string, string> container) =>
    AmbienceHub.CaptureCurrentValuesTo(
      container,
      flowingContractName
    )
);
```

### 15.4 Conceptual server bridge

The server-side integration is conceptually:

```csharp
sideChannelConfiguration.AcceptUjmwUnderlineProperty();

sideChannelConfiguration.ProcessDataVia(
  (IEnumerable<KeyValuePair<string, string>> container) =>
    AmbienceHub.RestoreValuesFrom(container)
);
```

or with a FlowingContract:

```csharp
sideChannelConfiguration.ProcessDataVia(
  (IEnumerable<KeyValuePair<string, string>> container) =>
    AmbienceHub.RestoreValuesFrom(
      container,
      flowingContractName
    )
);
```

### 15.5 Why this integration is powerful

The UJMW operation itself remains typed and business-oriented.

The ambient sidechannel can carry:

- tenancy;
- call-tree identity;
- infrastructure locality;
- other cross-cutting state.

A downstream service can then invoke another downstream service and capture the **already restored** context again.

This gives multi-hop propagation:

```text
Service A
  ambient tenant = X
        │
        │ UJMW sidechannel
        ▼
Service B
  ambient tenant = X
        │
        │ UJMW sidechannel
        ▼
Service C
  ambient tenant = X
```

No service method in this chain needs a `tenantId` parameter solely for infrastructure propagation.

### 15.6 Do not couple SmartAmbience to UJMW

An agent MUST NOT redesign `AmbienceHub` around UJMW DTOs or HTTP headers.

The UJMW bridge is one adapter around the generic capture/restore contract.

---

## 16. Infrastructure-Agnostic Tenancy

### 16.1 Tenancy above service topology

One of the strongest SmartAmbience use cases is an infrastructure-agnostic tenant context.

The architectural objective is:

> **Tenant identity belongs to the logical operation, not to the shape of the service graph.**

Therefore tenant propagation should remain correct when:

- one service is split into several services;
- several services are merged;
- an additional internal hop is introduced;
- an intermediate cross-tenant/shared service is inserted;
- the deployment topology changes;
- service URLs change;
- a call is routed through another infrastructure zone.

### 16.2 Multi-hop example

```text
User request
   │
   ▼
Frontend service
   ambient Tenant = Customer A
   │
   ▼
Shared processing service
   ambient Tenant = Customer A
   │
   ▼
Tenant-scoped data service
   ambient Tenant = Customer A
```

The middle service may itself be physically shared across all tenants.

The logical tenant context still flows through it.

### 16.3 Architectural payoff

This makes tenancy less dependent on:

- controller route design;
- explicit parameter threading;
- physical deployment boundaries;
- client-specific service wrappers;
- manually tenant-aware forwarding methods.

A service can be refactored without having to redesign every business contract simply to preserve tenant context.

### 16.4 Tenant-aware infrastructure behavior

Once restored, tenant context can influence infrastructure behavior such as:

- selecting tenant-specific configuration;
- selecting tenant-specific caches;
- resolving tenant-specific service targets;
- binding tenant-scoped dependencies;
- selecting a tenant-specific repository/provider;
- evaluating authorization scopes.

This is intentional.

The business method still remains focused on business inputs.

---

## 17. Call-Tree / Correlation Context

### 17.1 Beyond one service's call stack

Normal in-process stack traces end at process/service boundaries.

A distributed system still needs a way to group all operations belonging to one logical call tree.

A flowing call-tree identifier can provide that grouping key.

### 17.2 Example

```text
CallTreeGuid = G

Service A logs:
  G / operation 1
  G / operation 2

Service B logs:
  G / operation 3

Service C logs:
  G / operation 4
```

A centralized logging/data warehouse can group by `G` and reconstruct the logical distributed call tree.

### 17.3 Relationship to tracing standards

This concept is related to distributed tracing and W3C Trace Context.

SmartAmbience does not need to replace standardized tracing.

A project may choose to flow:

- a framework-specific call-tree identifier;
- W3C/OpenTelemetry trace identifiers;
- both.

The important SmartAmbience concept is that such infrastructure identifiers can participate in the same ambient dataflow model as tenancy and other context.

---

## 18. Infrastructure-Zone and Discovery Context

### 18.1 Context can influence the next hop

Ambient state is not limited to passive diagnostic metadata.

It can inform infrastructure decisions.

One important example is an **origin/infrastructure zone** associated with the caller.

### 18.2 Multi-hop discovery scenario

Consider:

```text
tenant-specific caller
        │
        ▼
shared cross-tenant service
        │
        ▼
needs another tenant-scoped service
```

The shared service may not have a hardcoded URL for the correct target.

Instead, its service-discovery layer can consider:

- the current tenant;
- the flowed infrastructure/origin zone;
- current deployment/discovery metadata.

Conceptually:

```text
ambient TenantId
+ ambient InfrastructureZone
+ service identity
        │
        ▼
service discovery
        │
        ▼
correct tenant-scoped endpoint URL
```

This allows target URLs to be resolved contextually rather than becoming business inputs.

### 18.3 Architectural consequence

A downstream call can be selected based on context that “arrived from above” without the business method explicitly receiving routing metadata.

This is a powerful form of infrastructure decoupling.

An agent MUST nevertheless keep this behavior understandable and testable.

Invisible context should not become an excuse for arbitrary hidden business rules.

---

## 19. Ambient Scope as Instance/Cache Partition

### 19.1 The lifetime gap

Two common instance lifetimes are often too coarse or too narrow:

```text
per call/request
  └── excellent isolation
      but limited reuse

process/application singleton
  └── excellent reuse
      but dangerous for tenant-specific state
```

A logical ambient binding scope can sit between them:

```text
per request
      <
ambient tenant/logical scope
      <
whole process/application
```

For many caches this is close to an ideal partition.

### 19.2 Desired pattern

SmartAmbience does not itself need to own the cache or singleton.

Instead:

```text
AmbientField-based binding identifier
        │
        ▼
semantic scope getter
        │
        ▼
cache/singleton/DI instance manager
        │
        ▼
instance selected by current logical scope
```

### 19.3 Independent scoped-singleton helper

A scoped-singleton helper can be wired explicitly to a scope getter, for example through a lambda that reads a semantic property backed by an AmbientField.

The exact helper API is outside the SmartAmbience core.

The important bridge is:

> **Instance management receives its partition key from the current ambient binding scope.**

### 19.4 DI-container integration

In larger systems the same idea can be implemented below application code by hooking a DI/container instance-management layer.

The container can then select cached/scoped instances using the ambient scope.

Again, SmartAmbience supplies context; the DI framework owns instances.

### 19.5 Why this can outperform global tenant-keyed caches

A global cache requires every key to correctly include the tenant dimension:

```text
GlobalCache[
  TenantId + ":" + BusinessKey
]
```

One forgotten tenant dimension can leak data.

With scope-partitioned instances:

```text
Cache instance for Tenant A:
  [BusinessKey]

Cache instance for Tenant B:
  [BusinessKey]
```

The tenant separation exists at the instance boundary.

This leads to an important design preference:

> **When feasible, structural scope isolation is safer than relying only on key discipline.**

### 19.6 Why this can outperform request-local caches

A request-local cache is destroyed after one call.

If multiple related operations under the same logical/tenant scope repeatedly need the same expensive data, that can waste work.

An ambient-partitioned cache can provide:

- higher reuse;
- bounded tenant isolation;
- less repeated loading;
- less memory duplication than indiscriminate per-request caches.

### 19.7 Agent rule

When designing a tenant-sensitive cache, an agent SHOULD consider whether ambient scope partitioning provides a better lifetime than:

- one global singleton;
- one cache per individual request.

It MUST NOT assume SmartAmbience itself is the cache manager.

---

## 20. Snapshotting for Scheduled and Deferred Work

### 20.1 Dataflow across time

Capture/Restore also supports temporal boundaries.

A logical operation may schedule work to execute later.

At scheduling time:

```text
current application/ambient state
        │
        ▼
capture snapshot
        │
        ▼
persist with scheduled work item
```

At execution time:

```text
scheduled work item
        │
        ▼
load snapshot
        │
        ▼
restore ambience
        │
        ▼
execute under reconstructed context
```

### 20.2 Mass-processing example

A planned mass process may be created while:

- a tenant is selected;
- a profile/scope is active;
- infrastructure discovery context is known;
- other ambient application state is set.

Instead of manually serializing each contextual subsystem independently, the relevant ambience can be captured as one snapshot and restored when the scheduled process starts.

### 20.3 “Freeze and thaw” mental model

A useful conceptual phrase is:

```text
freeze contextual application state
        ↓
persist/transport
        ↓
thaw into a later execution context
```

This does **not** mean arbitrary process memory or object graphs are serialized.

Only the ambient state exposed through the AmbienceHub contract is captured.

### 20.4 Determinism warning

A persisted snapshot can outlive code/configuration changes.

Applications using long-lived snapshots SHOULD consider:

- field/endpoint naming stability;
- backward compatibility;
- whether all captured values remain meaningful later;
- whether values should be revalidated;
- whether security context must be re-established independently.

---

## 21. Security and Trust Boundary

### 21.1 Fundamental rule

> **SmartAmbience is not an authentication or authorization mechanism.**

Captured/restored values are contextual data.

They are not automatically trustworthy.

### 21.2 Tenant context is not proof of tenant permission

An incoming sidechannel might say:

```text
TenantId = A
```

That fact alone MUST NOT authorize the caller for tenant A.

### 21.3 Recommended layered sequence

A robust remote-call architecture can follow this order:

```text
1. request arrives
        │
        ▼
2. authenticate caller/token
        │
        ▼
3. accept/parse sidechannel
        │
        ▼
4. restore SmartAmbience
        │
        ▼
5. tenant/scope context is now available
        │
        ▼
6. authorize the requested tenant/scope/arguments
   against claims/scopes from the authenticated token
        │
        ▼
7. invoke business method
```

### 21.4 Why restore before scope authorization can be useful

Some authorization decisions need both:

- authenticated token scopes;
- restored ambient tenant/scope;
- explicit method arguments.

The ambient restore makes the contextual dimension available.

A pre-invocation security hook can then validate all dimensions together.

### 21.5 Token/authorization layer has the last word

Conceptually:

```text
requested data filter
        +
restored ambient tenant/scope
        +
authenticated token scopes
        │
        ▼
authorization decision
```

The authorization/token layer determines whether the operation is allowed.

SmartAmbience only makes the relevant context available.

### 21.6 Argument-level authorization

A framework can additionally mark selected business arguments as requiring specific token scopes.

A pre-invocation evaluator can compare:

- contract method;
- concrete argument values;
- restored ambient tenant;
- authenticated scopes.

This is a good example of SmartAmbience enabling security evaluation without becoming the security system itself.

### 21.7 Trust crossing

When snapshots cross a trust boundary:

- authenticate the sender as appropriate;
- protect transport integrity/confidentiality as appropriate;
- validate authorization separately;
- do not restore arbitrary untrusted ambient values and assume they are safe simply because the schema is valid.

---

## 22. Failure Modes and Dangerous Designs

### 22.1 “Just use AsyncLocal”

Wrong assumption:

> `AsyncLocal` always survives every runtime/framework execution pattern.

Correct approach:

> Understand the host execution model and use a safe adapter when a host-specific rescue context is required.

### 22.2 Global fallback for request state

Wrong:

```text
Dictionary<string,string> GlobalAmbientValues
```

shared by all requests.

This can mix tenants and contexts.

### 22.3 Over-wide adapter scope

Wrong:

> Choose the most persistent available store.

Correct:

> Choose the narrowest store that reliably represents the same logical call across the required transitions.

### 22.4 Adapter `IsUsable` only checks null

A context object can be non-null but disposed.

The adapter must reflect real usability.

### 22.5 Confusing local survival with distributed flow

Wrong:

> If AmbientField is robust, it automatically exists in another service.

Correct:

> Another process/service requires explicit Capture → transport → Restore.

### 22.6 Putting infrastructure into every business signature

Wrong:

```csharp
DoWork(data, tenantId, callTreeGuid, infrastructureZone)
```

when those values are ambient infrastructure context.

### 22.7 Treating ambient tenant as authorization

Wrong:

```text
incoming tenant = A
therefore caller is authorized for A
```

Correct:

```text
incoming tenant = A
+
authenticated token authorizes A
→ operation may proceed
```

### 22.8 Making FlowingContracts mandatory

Wrong:

> Every application must define multiple named contracts.

Correct:

> Use them only when explicit selection provides value.

### 22.9 Global cache with forgotten tenant key

Wrong:

```text
GlobalCache[customerId]
```

for tenant-specific customer data.

Prefer an appropriate tenant/logical-scope partition or explicitly tenant-safe keying.

### 22.10 Treating custom endpoints as slow business services

Restore handlers execute as infrastructure context restoration.

Do not perform avoidable heavy business work there.

### 22.11 Assuming all fields exist before restore

Lazy initialization makes this unsafe.

Preserve pre-staging.

### 22.12 Assuming snapshot data is permanent truth

A snapshot represents context at capture time.

When restored much later, some values may require revalidation or recalculation.

---

## 23. Advanced `AmbientField` Capabilities

### 23.1 Context sealing

Use when a value must become immutable within the active context after initial binding.

Possible examples:

- high-integrity binding identifiers;
- environments where rebinding mid-call must fail loudly.

Do not use by default.

### 23.2 Termination callback

`OnTerminatingMethod` can receive the dying ambient value when the underlying context is terminating.

Potential uses include:

- scoped cleanup;
- diagnostics;
- releasing external state tied to the logical context.

The callback must remain safe and bounded.

### 23.3 Temporary branch execution

A temporary override can model a deliberate nested branch.

This must not become a general substitute for explicit scope design.

### 23.4 Debug information

Use diagnostic state when validating a new adapter or diagnosing unexpected context loss.

An agent investigating a bug SHOULD distinguish:

```text
inner async value lost
host fallback still available
```

from:

```text
host context itself no longer usable
```

because these imply different root causes.

---

## 24. Related Concepts and External Context

SmartAmbience should be understood in relation to established ambient/distributed-context concepts, while remaining its own architecture.

### 24.1 .NET `ExecutionContext`

Microsoft describes `ExecutionContext` as a container for ambient state that flows with the logical control flow of a program.

Reference:

- https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/executioncontext-synchronizationcontext
- https://learn.microsoft.com/dotnet/api/system.threading.executioncontext

This is directly relevant to understanding why thread-local storage alone is insufficient in asynchronous applications.

### 24.2 .NET `AsyncLocal<T>`

`AsyncLocal<T>` represents ambient data local to a particular asynchronous control flow.

Reference:

- https://learn.microsoft.com/dotnet/api/system.threading.asynclocal-1

SmartAmbience uses the same general ambient-context idea but adds host-specific recovery/fallback and generic distributed/deferred snapshot flow.

### 24.3 OpenTelemetry Context

OpenTelemetry defines a `Context` abstraction used to carry values across API boundaries and execution.

Reference:

- https://opentelemetry.io/docs/specs/otel/context/

This is conceptually related to SmartAmbience's logical-context model.

### 24.4 OpenTelemetry Propagators

OpenTelemetry Propagators define transport-specific **Inject** and **Extract** operations to write/read context through carriers such as HTTP headers.

Reference:

- https://opentelemetry.io/docs/specs/otel/context/api-propagators/

This provides a useful analogy:

```text
OpenTelemetry:
  Context → Inject → Carrier → Extract → Context

SmartAmbience:
  Ambience → Capture → Carrier → Restore → Ambience
```

The abstractions are related but not identical.

### 24.5 W3C Trace Context

W3C Trace Context standardizes HTTP propagation of distributed tracing metadata.

Reference:

- https://www.w3.org/TR/trace-context/

A SmartAmbience call-tree/correlation use case is related to this domain.

Where standardized tracing already exists, an application should consider integrating rather than unnecessarily reinventing trace propagation.

### 24.6 W3C Baggage

W3C Baggage standardizes propagation of application-defined properties associated with a distributed request/workflow.

Reference:

- https://www.w3.org/TR/baggage/

This is conceptually close to generic ambient key/value propagation.

However, SmartAmbience additionally addresses:

- local context survival through technology-specific adapters;
- application-specific exposed AmbientFields;
- custom endpoint capture/restore;
- restore ordering;
- optional FlowingContracts;
- pre-staging;
- using ambient scope as an instance/cache binding source;
- non-network snapshot/restore scenarios.

### 24.7 Do not replace SmartAmbience semantics with a superficially similar standard

An agent MUST NOT conclude:

> “OpenTelemetry/W3C already has context propagation, therefore SmartAmbience is unnecessary.”

Nor should it conclude the reverse.

These systems solve overlapping but not identical problems.

Choose integration points based on concrete architecture.

---

## 25. Agent Design Rules

### 25.1 MUST

An AI agent MUST:

- treat SmartAmbience as logical context infrastructure, not just a convenience wrapper;
- preserve strict isolation between unrelated calls;
- understand the difference between local context survival and distributed/deferred dataflow;
- initialize an appropriate `ContextAdapter` before normal AmbientField use;
- keep raw AmbientFields encapsulated behind semantic properties by default;
- preserve exposed-field name stability when snapshots must interoperate;
- preserve pre-staging behavior for lazy field construction;
- treat AmbienceHub snapshots as transport-agnostic key/value state;
- keep SmartAmbience independent from business transport semantics;
- distinguish `null` restore input from an empty snapshot where the framework does;
- preserve custom endpoint namespace separation;
- identify restore dependencies when custom endpoints depend on other ambient state;
- keep restore handlers deterministic and suitably fast;
- treat FlowingContracts as optional;
- distinguish context propagation from authentication/authorization;
- revalidate security at the proper security boundary;
- reason about cache/singleton scope isolation when ambient binding identifiers are used for instance partitioning;
- analyze a hosting technology before generating a new context adapter;
- test new adapters for parallel cross-call isolation.

### 25.2 SHOULD

An AI agent SHOULD:

- use a semantic state-manager/facade class around AmbientField values;
- use the narrowest reliable host context as adapter fallback;
- use context adapter diagnostics when investigating lost ambience;
- use explicit snapshot/restore around scheduling or queue boundaries;
- use UJMW sidechannels when UJMW is already the transport framework;
- prefer sidechannel context to pollution of business method signatures;
- consider standardized tracing integration for tracing-specific state;
- consider ambient scope partitioning for tenant-sensitive caches;
- keep FlowingContract count small unless multiple dataflow boundaries genuinely need different state subsets;
- document why a custom endpoint exists and what it owns.

### 25.3 MUST NOT

An AI agent MUST NOT:

- replace SmartAmbience with a process-global dictionary for request/tenant state;
- assume thread identity equals logical context identity;
- assume `AsyncLocal` can never be lost;
- create an adapter without investigating host concurrency/lifetime behavior;
- use a host context after it is disposed/unusable;
- allow two concurrent logical requests to share one contextual value store;
- treat a restored tenant/scope as proof of authorization;
- move authorization semantics into AmbientField itself;
- add tenant/correlation/routing arguments to business methods merely for propagation;
- assume SmartAmbience manages scoped singleton instances itself;
- expose internal enterprise-specific component names when documenting general patterns;
- turn compatibility/legacy setup into the normative baseline;
- make advanced features such as sealing or multiple FlowingContracts mandatory boilerplate.

---

## 26. Canonical Usage Examples

### 26.1 Encapsulated exposed field

```csharp
using System.Threading;

public static class TenantContext {

  private static readonly AmbientField _CurrentTenant =
    new AmbientField("CurrentTenant", true);

  public static string CurrentTenant {
    get {
      return _CurrentTenant.Value;
    }
    set {
      _CurrentTenant.Value = value;
    }
  }

}
```

This illustrates the preferred encapsulation pattern.

The concrete field name and type conversion belong to the application.

### 26.2 Capture snapshot

```csharp
using DistributedDataFlow;
using System.Collections.Generic;

Dictionary<string, string> snapshot =
  new Dictionary<string, string>();

AmbienceHub.CaptureCurrentValuesTo(snapshot);
```

### 26.3 Restore snapshot

```csharp
AmbienceHub.RestoreValuesFrom(snapshot);
```

### 26.4 Custom endpoint

Conceptual example:

```csharp
AmbienceHub.BindCustomEndpoint(
  "DiscoveryContext",
  (Action<string, string> capture) => {
    capture("OriginZone", DiscoveryState.OriginZone);
  },
  (IEnumerable<KeyValuePair<string, string>> incoming) => {
    foreach (KeyValuePair<string, string> entry in incoming) {
      if (entry.Key == "OriginZone") {
        DiscoveryState.OriginZone = entry.Value;
      }
    }
  }
);
```

This is intentionally simple.

Production code should handle its domain's validation and missing values explicitly.

### 26.5 Optional FlowingContract

```csharp
AmbienceHub.DefineFlowingContract(
  "default",
  (FlowingContractDefinition flow) => {
    flow.IncludeAllExposedAmbientFieldInstances();
    flow.IncludeAllCustomEndpoints();
  }
);
```

Capture:

```csharp
AmbienceHub.CaptureCurrentValuesTo(
  snapshot,
  "default"
);
```

Restore:

```csharp
AmbienceHub.RestoreValuesFrom(
  snapshot,
  "default"
);
```

### 26.6 Scheduled-work snapshot

Conceptual:

```csharp
Dictionary<string, string> ambientSnapshot =
  new Dictionary<string, string>();

AmbienceHub.CaptureCurrentValuesTo(ambientSnapshot);

ScheduledWorkItem item = new ScheduledWorkItem();
item.AmbientSnapshot = ambientSnapshot;
item.Payload = businessPayload;

scheduler.Store(item);
```

Later:

```csharp
ScheduledWorkItem item = scheduler.Load(workItemId);

AmbienceHub.RestoreValuesFrom(item.AmbientSnapshot);

processor.Execute(item.Payload);
```

The persistence model is illustrative.

### 26.7 UJMW sidechannel

Conceptually:

```csharp
UjmwClientConfiguration.ConfigureRequestSidechannel(
  (Type contractType, OutgoingRequestSideChannelConfiguration sideChannel) => {

    sideChannel.ProvideUjmwUnderlineProperty();

    sideChannel.CaptureDataVia(
      (IDictionary<string, string> snapshot) => {
        AmbienceHub.CaptureCurrentValuesTo(snapshot);
      }
    );

  }
);
```

Server concept:

```csharp
UjmwHostConfiguration.ConfigureRequestSidechannel(
  (Type contractType, IncommingRequestSideChannelConfiguration sideChannel) => {

    sideChannel.AcceptUjmwUnderlineProperty();

    sideChannel.ProcessDataVia(
      (IEnumerable<KeyValuePair<string, string>> snapshot) => {
        AmbienceHub.RestoreValuesFrom(snapshot);
      }
    );

  }
);
```

These examples intentionally show only the SmartAmbience bridge.

Real UJMW applications can apply contract-specific configuration, optional channels, defaults, authentication, and authorization separately.

---

## 27. New Context Adapter — Agent Workflow

When asked to support a new hosting technology, the agent SHOULD follow this sequence.

### 27.1 Step 1 — identify the logical scope

Determine what SmartAmbience values are meant to belong to.

Examples:

```text
one incoming request
one RPC operation
one message-consumption operation
one scheduled execution
one desktop command scope
```

### 27.2 Step 2 — study host execution

Determine:

- threading;
- async continuation;
- scheduling;
- context suppression;
- request reuse/pooling;
- disposal;
- nesting;
- cancellation;
- termination callbacks.

### 27.3 Step 3 — find candidate rescue context

Look for a host-provided object that:

- is available to all execution belonging to the logical scope;
- remains unavailable to unrelated scopes;
- can store key/value state or reference a store;
- has a well-defined lifetime.

### 27.4 Step 4 — prove isolation

Before coding, answer:

```text
If request A and request B execute at exactly the same time,
what guarantees that adapter storage for A cannot be read by B?
```

If there is no convincing answer, the design is not ready.

### 27.5 Step 5 — implement usability

Model disposed/invalid state accurately.

Do not let stale references create false `IsUsable == true`.

### 27.6 Step 6 — implement get/set

Use the same per-context store for both.

Preserve null/absence semantics expected by SmartAmbience.

### 27.7 Step 7 — implement termination

Raise termination while contextual values can still be read if the host allows it.

### 27.8 Step 8 — stress-test

Run parallel operations with different tenant/scope values.

Introduce:

- awaits;
- thread-pool hops;
- cancellation;
- exceptions;
- nested operations;
- disposal.

Assert that values remain correct and never cross.

### 27.9 Step 9 — document limitations

If some host scheduling pattern cannot preserve ambient state safely, document that explicitly.

A known limitation is better than an unsafe pseudo-solution.

---

## 28. Developing a Custom Endpoint — Agent Workflow

### 28.1 Identify endpoint ownership

Define exactly which contextual subsystem the endpoint owns.

Avoid overlapping ownership with existing endpoints.

### 28.2 Define stable endpoint-local keys

Keys should be:

- stable;
- meaningful;
- deterministic.

Do not include ephemeral object hashes or process-specific identity unless that is intentionally the data being transported.

### 28.3 Capture only contextual state

Do not capture arbitrary business object graphs.

The endpoint should represent infrastructure/application context relevant to reconstruction.

### 28.4 Define restore semantics

For each key determine:

- missing key behavior;
- invalid value behavior;
- default behavior;
- whether restore replaces or merges state;
- whether order matters.

### 28.5 Declare dependencies

If endpoint B requires endpoint A restored first, use `restoreAfter` rather than assuming registration order.

### 28.6 Keep restore bounded

Avoid expensive I/O unless the architecture explicitly requires it.

If reconstruction depends on remote work, consider whether the snapshot should contain enough state to avoid that dependency.

### 28.7 Test roundtrip

Capture → clear/new context → restore → verify equivalent contextual behavior.

---

## 29. Migration and Adoption Guidance

This section is intentionally secondary to the desired state.

### 29.1 Explicit tenant parameter threading → AmbientField

Before:

```csharp
Order LoadOrder(
  long orderId,
  string tenantId
);
```

if `tenantId` exists only as infrastructure binding context.

Desired direction:

```csharp
Order LoadOrder(long orderId);
```

and obtain the tenant from an encapsulated ambient state manager.

Do **not** remove an explicit tenant parameter when it is genuinely part of business semantics.

### 29.2 Plain AsyncLocal → AmbientField

When a plain `AsyncLocal` has shown context-loss problems under a host:

1. identify the required logical scope;
2. implement/select the correct context adapter;
3. replace raw ambient storage with a semantic state facade backed by `AmbientField`;
4. stress-test context switching and parallel requests.

Do not mechanically replace every AsyncLocal merely because SmartAmbience exists.

### 29.3 Ad-hoc HTTP headers → AmbienceHub bridge

If several ambient values are manually copied into custom headers:

1. identify which values are true ambient infrastructure context;
2. expose them through SmartAmbience;
3. capture them through `AmbienceHub`;
4. let the transport encode the snapshot;
5. restore the snapshot on receive;
6. keep security validation separate.

### 29.4 Request-local cache → ambient-scope cache

If request-local cache churn is high but data must remain tenant-isolated:

1. identify a stable ambient binding identifier;
2. make the cache manager use it as its partition/scope key;
3. define cleanup/retention behavior;
4. test that tenant A and tenant B receive different cache instances.

### 29.5 Global tenant-keyed cache → scope-partitioned cache

Before changing:

- verify memory impact;
- define scope lifecycle;
- define eviction;
- confirm the ambient identifier is stable and correct;
- preserve thread safety inside each cache instance.

SmartAmbience does not solve cache eviction automatically.

### 29.6 Scheduled work

When converting immediate execution to deferred execution:

1. identify ambient state required later;
2. capture it when scheduling;
3. persist it with the work item;
4. restore it before running;
5. re-establish authentication/authorization independently if required;
6. consider snapshot-version compatibility for long delays.

---

## 30. Scope Boundaries

### 30.1 In scope

This document intentionally covers:

- AmbientField semantics;
- context-switch robustness;
- technology-specific adapters;
- adapter implementation guidance;
- strict call isolation;
- exposed fields;
- pre-staging;
- AmbienceHub capture/restore;
- custom endpoints;
- restore ordering;
- optional FlowingContracts;
- transport-agnostic dataflow;
- UJMW sidechannel integration as an example;
- infrastructure-agnostic tenancy;
- multi-hop context propagation;
- call-tree/correlation use cases;
- infrastructure-zone/discovery use cases;
- ambient scope as external cache/instance partition;
- deferred/scheduled snapshot use;
- security/trust boundary;
- agent generation/review rules.

### 30.2 Out of scope

This document does not define:

- a concrete authentication-token format;
- a complete authorization framework;
- concrete enterprise product/component names;
- proprietary service-discovery implementations;
- proprietary logging-data-warehouse schemas;
- concrete enterprise tenant/profile manager implementations;
- legacy core-library compatibility architecture;
- a particular DI container;
- a particular scoped-singleton helper implementation;
- the complete UJMW protocol;
- complete OpenTelemetry implementation guidance;
- application-specific business scopes;
- cache eviction algorithms;
- arbitrary application-state serialization outside AmbienceHub-exposed state.

### 30.3 UJMW boundary

For exact UJMW protocol behavior, use the separate UJMW skill/specification.

This SmartAmbience document only explains the integration boundary necessary to understand ambient dataflow.

---

## 31. Source-Derived Invariants

The reviewed SmartAmbience implementation establishes the following relevant invariants.

### 31.1 AmbientField

- an AmbientField has a stable `Name`;
- non-exposed instances use a unique internal fallback key;
- exposed instances use their name as the external key;
- exposed instances register in a central exposed-instance catalog;
- the inner local carrier uses `AsyncLocal<string>`;
- an external `IAmbienceToSomeContextAdapter` can store/recover values;
- the adapter must be initialized before normal field use;
- an explicit null value is represented internally by a sentinel;
- a usable host context is preferred over the long-living fallback;
- the async-local value is always updated on set;
- if the async-local value is missing but host context is usable, the value is recovered and the async-local carrier is repopulated;
- exposed values can be pre-staged before field construction;
- a context value can be sealed;
- a temporary local branch override is supported;
- diagnostics retain information about recent retrieval behavior;
- a termination hook can receive a dying context value.

### 31.2 Context adapter

- exposes `IsUsable`;
- supports set/get by string key;
- exposes a context-termination event;
- the adapter is effectively a global SmartAmbience configuration point while the values it accesses are scoped by the adapter's current host context;
- changing an already configured adapter to a different adapter type is intentionally restricted by the current implementation.

### 31.3 AmbienceHub

- AmbientField integration is always bound as a hub endpoint;
- capture can target a dictionary or a callback;
- restore accepts enumerable key/value data;
- custom endpoints can participate in every snapshot;
- normal dedicated endpoints have their endpoint name added as a key prefix;
- duplicate capture keys are rejected;
- endpoint restore order can depend on other endpoints;
- restore duration can be observed against an expected threshold;
- null restore input has configurable behavior and differs from empty input;
- FlowingContracts can select fields/endpoints;
- without configured FlowingContracts, a built-in default definition is available;
- once explicit FlowingContracts are configured, a contract name is required;
- contract names are normalized by a lowercase requirement in the current implementation;
- contract definitions become immutable after setup.

### 31.4 ASP.NET Core integration

- the adapter stores host values in `HttpContext.Items`;
- the current HttpContext is tracked through an async-local reference;
- disposed contexts are treated as unusable;
- middleware establishes and clears the current host context;
- termination is raised before the adapter's current context is cleared;
- setup assigns the middleware adapter to `AmbientField.ContextAdapter`.

These invariants describe the reviewed implementation.

Future versions of this document may refine which are treated as permanent public contract versus current implementation behavior.

---

## 32. Requirements Summary

| Area | Desired State | Strength |
|---|---|---|
| Ambient context | Logical execution context, not thread identity | MUST |
| Isolation | Unrelated calls never share ambient state | MUST |
| Local robustness | Recover via safe host-specific context where possible | MUST |
| Context adapter | Technology-specific and lifecycle-aware | MUST |
| Adapter generation | Agent can design/implement new adapters | MUST |
| Adapter fallback | Narrowest reliable logical host context | MUST |
| Unsafe global fallback | Never for call/tenant isolation | MUST NOT |
| AmbientField exposure | Raw field private; semantic property preferred | SHOULD |
| Exposed field names | Stable and meaningful | MUST |
| Pre-staging | Preserve lazy-init restore behavior | MUST |
| AmbienceHub | Canonical capture/restore orchestrator | MUST |
| Snapshot shape | String key/value oriented | MUST |
| Custom endpoints | Supported for non-field contextual subsystems | MAY |
| Endpoint namespace | Avoid collisions; dedicated prefixing | MUST |
| Restore dependencies | Declare explicitly when required | MUST |
| FlowingContracts | Optional advanced selection | MAY |
| Multiple FlowingContracts | Only when different boundaries need them | SHOULD NOT by default |
| Transport | SmartAmbience remains transport-agnostic | MUST |
| UJMW | Concrete compatible sidechannel example | SHOULD when applicable |
| `_` property | UJMW sidechannel carrier, not business parameter | MUST when using that integration |
| Multi-hop tenancy | First-class use case | SHOULD |
| Call-tree identity | First-class distributed diagnostic use case | SHOULD |
| Infrastructure zone | Valid ambient routing/discovery context | MAY |
| Scheduled snapshots | Valid freeze/thaw use case | MAY |
| Cache/instance binding | External manager may use ambient scope key | MAY |
| SmartAmbience as DI/cache manager | Incorrect model | MUST NOT |
| Security | Separate from context propagation | MUST |
| Ambient tenant as authorization | Never sufficient | MUST NOT |
| Authorization timing | May evaluate restored scope against authenticated token | SHOULD |
| Enterprise internals | Generalize patterns; do not expose proprietary names | MUST |

---

## 33. Review Checklist for AI Agents

Before generating or approving SmartAmbience-related code, verify:

### 33.1 AmbientField usage

- Is the field really infrastructure context?
- Is the raw field encapsulated?
- Does it need to be exposed for capture/restore?
- Is its exposed name stable?
- Is null behavior understood?
- Does this code require `AssertLongLivingValueIsNotUsed()`?

### 33.2 Adapter

- What is the logical scope?
- What host object is the rescue anchor?
- Can parallel requests see the same store?
- What happens on disposal?
- Is `IsUsable` accurate?
- Is termination raised correctly?
- Have cross-call concurrency tests been designed?

### 33.3 Dataflow

- Is capture/restore the right boundary?
- Does the carrier preserve all key/value pairs?
- Is a FlowingContract actually necessary?
- Are missing/null snapshot semantics explicit?
- Are custom endpoints deterministic?
- Is restore ordering correct?

### 33.4 Remote call

- Are ambient values kept out of the business method signature?
- Does the transport have a clean sidechannel?
- Is the snapshot restored before code that depends on it?
- Is authorization performed independently?
- Can the downstream service re-capture the restored context for another hop?

### 33.5 Tenancy

- Is tenant identity logical context rather than accidental route state?
- Can the architecture survive an extra service hop?
- Can a shared service safely forward into tenant-specific infrastructure?
- Can two tenants execute concurrently without cache/context crossover?

### 33.6 Caching/instances

- Is global caching safe?
- Would ambient partitioning improve isolation?
- Who owns instance lifecycle and eviction?
- Is SmartAmbience merely supplying the scope key as intended?

### 33.7 Scheduling

- Which ambient values must survive until later?
- Can they be safely persisted?
- Will they still be valid when restored?
- Does security need to be re-established?
- Is snapshot compatibility across versions understood?

---

## 34. Architectural Examples

### 34.1 Tenant propagation independent of service decomposition

```text
Before refactoring:

Client
  → BillingService
      Tenant = A


After refactoring:

Client
  → BillingGateway
      Tenant = A
        → BillingRulesService
            Tenant = A
              → BillingStoreService
                  Tenant = A
```

No tenant propagation argument must be added merely because two new hops appeared.

### 34.2 Distributed call-tree grouping

```text
CallTreeGuid = 123...

API
  log(123..., "request accepted")

Rules service
  log(123..., "rules evaluated")

Data service
  log(123..., "query executed")

Notification service
  log(123..., "notification queued")
```

A central log system can group all entries by the same identifier.

### 34.3 Discovery based on ambient origin context

```text
Shared Service
  current tenant = A
  origin zone = EU-1

        │
        ▼
Resolve(
  service = "CustomerData",
  tenant = A,
  zone = EU-1
)

        │
        ▼
https://tenant-a.eu-1.example/...
```

The URL resolution is infrastructure behavior, not a business parameter.

### 34.4 Tenant-partitioned cache

```text
ambient Tenant = A
        │
        ▼
scope-aware instance resolver
        │
        ▼
CustomerMetadataCache[A]


ambient Tenant = B
        │
        ▼
scope-aware instance resolver
        │
        ▼
CustomerMetadataCache[B]
```

Each cache can use normal business keys internally without accidental cross-tenant reuse.

### 34.5 Scheduled mass process

```text
user/configuration context at scheduling time
        │
        ▼
AmbienceHub.CaptureCurrentValuesTo(...)
        │
        ▼
scheduled job record
        │
      hours later
        ▼
AmbienceHub.RestoreValuesFrom(...)
        │
        ▼
mass processing executes under restored context
```

---

## 35. Design Philosophy

SmartAmbience is powerful because it deliberately keeps the primitive small.

At its core:

```text
named ambient value
+
technology-specific rescue context
+
capture/restore of string key/value state
```

From these small primitives emerge larger architectural capabilities:

- robust ambient context;
- multi-tenant isolation;
- service-topology-independent tenancy;
- multi-hop distributed context;
- distributed call-tree grouping;
- infrastructure-aware routing;
- contextual service discovery;
- scoped caches and instances;
- deferred context restoration;
- compatibility with arbitrary transport mechanisms.

The architectural lesson is:

> **A small, well-defined logical context layer can remove cross-cutting infrastructure concerns from business contracts without forcing the application into one hosting, transport, DI, tracing, or service-discovery technology.**

The counterbalance is equally important:

> **Invisible context is only safe when lifetime, isolation, and trust boundaries are explicit.**

SmartAmbience should therefore be used with strong discipline around:

- scope definition;
- adapter correctness;
- parallel isolation;
- snapshot boundaries;
- authorization;
- lifecycle ownership.

---

## 36. Maintenance Rules

After this V1, this document MUST be maintained incrementally.

### 36.1 Structural stability

Future revisions SHOULD preserve:

- top-level section numbering;
- section order;
- established terminology;
- canonical examples where unchanged.

### 36.2 Diff quality

When new knowledge appears:

1. modify the smallest relevant section;
2. avoid rewriting unaffected prose;
3. add a subsection before adding a new top-level section;
4. preserve examples unless their semantics changed;
5. separate implementation discovery from desired-state promotion.

### 36.3 Evidence versus standard

A newly discovered SmartAmbience-related implementation is **evidence**, not automatically desired state.

Before promoting behavior to a rule, evaluate:

- Is it fundamental to context correctness?
- Is it public and reusable?
- Is it host-specific?
- Is it an advanced capability?
- Is it legacy compatibility?
- Does it preserve isolation?
- Does it preserve transport independence?
- Does it make the agent more reliable without overfitting one enterprise system?

### 36.4 Confidential/internal examples

Enterprise implementations may be used to derive:

- architectural patterns;
- stress cases;
- capability requirements;
- security lessons;
- performance constraints.

Future versions MUST continue to generalize these insights rather than exposing proprietary component names or internal topology.

### 36.5 External references

External standards and documentation in Section 24 are contextual references.

They may be updated when URLs or standards evolve without changing the SmartAmbience normative model unless a deliberate integration decision is made.

---

## 37. Reference Basis

This V1 is based on the reviewed SmartAmbience implementation and supplied architectural examples, including the following source concepts:

- `AmbientField`;
- `AmbientField.DebugInfo`;
- `AmbientField` test hooks;
- `IAmbienceToSomeContextAdapter`;
- a never-usable context adapter;
- an application-domain-style context adapter;
- `AmbienceHub`;
- `FlowingContractDefinition`;
- AmbienceHub/AmbientField wire-up;
- ASP.NET Core `AmbientFieldAdapterMiddleware`;
- ASP.NET Core application-builder setup extension;
- HTTP-side flowed-data extraction integration;
- compatibility with UJMW sidechannels;
- enterprise-derived multi-hop tenancy patterns;
- call-tree/correlation propagation;
- infrastructure-zone/discovery propagation;
- ambient scope used as a binding input for cache/singleton/DI instance management;
- scheduled/deferred snapshot restoration;
- post-restore authorization patterns.

The separate UJMW skill/specification remains authoritative for exact UJMW transport behavior.

---

## 38. Final Normative Position

SmartAmbience should be understood as a **logical ambient-context and dataflow infrastructure**.

Its local context model improves upon naïve `AsyncLocal` usage by combining local ambient flow with a technology-specific fallback/root context that can recover values after relevant context transitions.

That robustness is always subordinate to the stronger invariant:

> **Two unrelated logical calls must never be mixed.**

`AmbienceHub` extends the same logical state model across boundaries by capturing and restoring snapshots of exposed ambient state.

Those boundaries may be:

- service/process hops;
- transport changes;
- queues;
- schedulers;
- delayed/background execution;
- persisted mass-processing plans.

The framework is intentionally transport-agnostic.

UJMW is a particularly natural integration because its `_` sidechannel can carry the complete SmartAmbience snapshot separately from typed business arguments.

This enables infrastructure concerns such as tenant identity, call-tree identity, infrastructure location, and related scope state to flow through arbitrary service graphs without becoming part of business method signatures.

The resulting logical scope can also serve as an external binding/partition key for scoped caches, singleton helpers, or DI-container instance management, providing a lifetime that is often more useful than either “per request” or “global application singleton” while preserving tenant isolation.

SmartAmbience does not authenticate or authorize any of this state.

A restored tenant or scope is context, not permission.

Authentication and authorization layers must independently validate that the authenticated caller is allowed to operate under the restored context and with the requested method arguments.

For AI agents, the most important operational rule is therefore:

> **Preserve context aggressively, preserve isolation absolutely, keep context out of business signatures where it is infrastructure-only, and never confuse propagated context with trusted authorization.**
