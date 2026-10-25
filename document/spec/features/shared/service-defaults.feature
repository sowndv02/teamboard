@TB-001 @shared
Feature: ServiceDefaults shared by every service
  As a developer
  I want a reusable ServiceDefaults library
  So that every service gets the same health and telemetry without copying code

  @S06 @FR-002
  Scenario: Readiness check reports unhealthy before the application has finished starting
    Given a host using AddServiceDefaults that has not started
    When I run the health checks tagged "ready"
    Then the result is Unhealthy

  @S07 @FR-004
  Scenario: Another service using ServiceDefaults gets the same health endpoints
    Given a minimal host calling AddServiceDefaults and MapDefaultEndpoints that has started
    When I call both "GET /health/live" and "GET /health/ready"
    Then both responses have status 200
