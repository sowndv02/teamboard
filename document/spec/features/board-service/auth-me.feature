@TB-033 @board-service
Feature: board-service protected auth profile endpoint
  As an authenticated API client
  I want to call a protected endpoint with a Bearer access token
  So that only valid identity tokens are accepted

  Background:
    Given board-service JWT validation is configured for BDD tests

  @S01 @FR-006
  Scenario: Valid access token returns the token subject
    Given an access token was issued for user "a1111111-1111-1111-1111-111111111111"
    When I send GET to "/api/v1/auth/me" with that Bearer access token
    Then the auth me response status is 200
    And the response body has sub "a1111111-1111-1111-1111-111111111111"

  @S02 @FR-006
  Scenario: Missing Bearer token is rejected
    When I send GET to "/api/v1/auth/me" without an Authorization header
    Then the auth me response status is 401

  @S03 @FR-006
  Scenario: Wrong audience token is rejected
    Given an access token was issued for user "b2222222-2222-2222-2222-222222222222" with audience "wrong-aud"
    When I send GET to "/api/v1/auth/me" with that Bearer access token
    Then the auth me response status is 401

  @S04 @FR-006 @BR-004
  Scenario: Expired access token is rejected
    Given an expired access token was issued for user "c3333333-3333-3333-3333-333333333333"
    When I send GET to "/api/v1/auth/me" with that Bearer access token
    Then the auth me response status is 401