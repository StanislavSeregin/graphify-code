# GraphifyCode Data Repository Template

This repository serves as a data storage for microservice architecture analysis using the GraphifyCode tool.

## Structure

- `/graph-data/` - JSON files containing service analysis data
- `docker-compose.yml` - configuration for deploying GraphifyCode.Api and Frontend
- `.github/workflows/deploy.yml` - CI/CD pipeline for automatic deployment

## Usage

1. Clone this repository
2. Configure GraphifyCode.MCP to work with this folder
3. Use Claude Code with MCP to analyze your codebase
4. Commit changes to git - the application will automatically update via CI/CD

## MCP Setup

See documentation in the main GraphifyCode repository.