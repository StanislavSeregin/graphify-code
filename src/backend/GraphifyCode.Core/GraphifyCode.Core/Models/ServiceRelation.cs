using System;

namespace GraphifyCode.Core.Models;

public record ServiceRelation(Guid SourceServiceId, Guid TargetEndpointId);
