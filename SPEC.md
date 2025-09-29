# Interactive Codebase Graph Construction and Visualization

## Technology Stack

- Backend: C#
- Frontend: Angular + TS
- Contract Language for Documentation: TS

## Goals

- Show microservices and their purpose
- Show entry points in microservices with descriptions
- Show connections between microservices
- Allow quick navigation to code

## Service Interaction Representation

```ts
type ServiceGraph = {
    services: Array<ServiceNode>,
};

type ServiceNode = {
    id: string,
    name: string,
    description: string,
    relations: Array<ServiceRelation>,
    metadata: AnalysisMetadata
};

type ServiceRelation = {
    sourceServiceId: string,
    targetEndpointId: string
}

type ServiceEndpoint = {
    serviceId: string,
    name: string,
    description: string,
    type: 'http' | 'queue' | 'job',
    metadata: AnalysisMetadata
}

type AnalysisMetadata = {
    lastAnalyzedAt: Date,           // last analysis timestamp
    sourceCodeHash?: string,        // for tracking code changes
    relativeCodePath?: string       // null for external integrations
}

// Data storage file schemas
type ServiceEndpoints = {
    endpoints: Array<ServiceEndpoint>
}

type ServiceRelations = {
    relations: Array<ServiceRelation>
}
```

## Data Files Structure

```
/graph-data/
  /service-name/
    index.json      # ServiceNode directly
    endpoints.json  # ServiceEndpoints
    relations.json  # ServiceRelations
```