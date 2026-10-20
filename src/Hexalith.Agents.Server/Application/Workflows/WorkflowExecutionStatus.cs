using System.Text.Json.Serialization;

namespace Hexalith.Agents.Server.Application.Workflows;

/// <summary>Content-free durable workflow execution outcome.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WorkflowExecutionStatus>))]
public enum WorkflowExecutionStatus
{
    /// <summary>Unknown outcome.</summary>
    Unknown = 0,
    /// <summary>Running outcome.</summary>
    Running = 1,
    /// <summary>Completed outcome.</summary>
    Completed = 2,
    /// <summary>Failed outcome.</summary>
    Failed = 3,
    /// <summary>Suppressed outcome.</summary>
    Suppressed = 4,
}
