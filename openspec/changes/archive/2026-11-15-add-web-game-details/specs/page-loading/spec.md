# Spec Delta

## Purpose

Provide consistent page progress across application shells, combining navigation activity with declaratively supplied feature loading states.

## ADDED Requirements

### Requirement: Combine navigation and page loading

Admin and web SHALL show an indeterminate header progress bar while navigation or any registered page loading source is active. The bar SHALL disappear only when all sources are inactive, without changing header dimensions.

#### Scenario: Navigation finishes before data

- **WHEN** navigation finishes while the destination page still fetches data
- **THEN** the bar remains visible until the page loading source becomes inactive

#### Scenario: Navigation terminates unsuccessfully

- **WHEN** navigation is cancelled or fails and no page loading source remains active
- **THEN** the bar disappears

### Requirement: Declare loading with page lifecycle

Features SHALL declare reactive loading states without imperative start/stop calls or a required header content template. Destroying a declaration SHALL release its contribution without clearing other sources. Existing admin header loading declarations SHALL retain their behavior.

#### Scenario: Leave a loading page

- **WHEN** a feature declaring active loading is destroyed
- **THEN** its contribution is removed and no loading state leaks into the next page

#### Scenario: Multiple declarations

- **WHEN** two declarations are active and one finishes or is destroyed
- **THEN** progress remains visible for the other active declaration

### Requirement: Reflect explicit web query activity

The web catalog and game detail page SHALL declare request activity for header progress, including detail retrieval while catalog placeholder data is displayed and explicit refetches. Progress SHALL NOT require global interception of all HTTP traffic.

#### Scenario: Fetch details over a cached card

- **WHEN** a cached card is visible while its complete detail is fetching
- **THEN** the header progress bar remains active until that fetch settles
