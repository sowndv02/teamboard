@TB-001 @board-service
Feature: Repository dependency and configuration hygiene
  As a developer
  I want pinned package versions and no hard-coded secrets
  So that builds are repeatable and secrets do not leak

  @S14 @NFR-003
  Scenario: Every NuGet package in the repo has a pinned version
    When I read the package declarations of the .NET projects
    Then every package has an exact version, with no ranges or wildcards

  @S15 @NFR-001
  Scenario: Compose files contain no hard-coded secrets
    When I read the compose files of the repo
    Then every password, key or token value only references an environment variable
