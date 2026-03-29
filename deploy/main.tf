terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=4.54.0"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

locals {
  tags = {
    env  = var.env
    name = "presettie"
  }
}

resource "azurerm_resource_group" "rg-presettie" {
  name     = "rg-presettie-${var.env}"
  location = "France Central"

  tags = local.tags
}

resource "azurerm_log_analytics_workspace" "appi-ws-presettie" {
  name                = "appi-ws-presettie-${var.env}"
  location            = azurerm_resource_group.rg-presettie.location
  resource_group_name = azurerm_resource_group.rg-presettie.name

  sku               = "PerGB2018"
  retention_in_days = 30

  tags = local.tags
}

resource "azurerm_application_insights" "appi-presettie" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location
  workspace_id        = azurerm_log_analytics_workspace.appi-ws-presettie.id

  name             = "appi-presettie-${var.env}"
  application_type = "web"
}

resource "azurerm_storage_account" "st-presettie" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  name                     = "stpresettie${var.env}"
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = local.tags
}

resource "azurerm_storage_container" "stc-bot-deployments-presettie" {
  storage_account_id = azurerm_storage_account.st-presettie.id

  name = "bot-deployments"
}

resource "azurerm_storage_container" "stc-api-deployments-presettie" {
  storage_account_id = azurerm_storage_account.st-presettie.id

  name = "api-deployments"
}

resource "azurerm_storage_queue" "stq-requests-presettie" {
  storage_account_id = azurerm_storage_account.st-presettie.id

  name = "requests"
}

# Identity

resource "azurerm_user_assigned_identity" "ado-pipeline-identity" {
  location            = azurerm_resource_group.rg-presettie.location
  name                = "ado-pipeline-identity-presettie-${var.env}"
  resource_group_name = azurerm_resource_group.rg-presettie.name
}

resource "azurerm_role_assignment" "ado-pipeline-identity-blob-access" {
  scope                = azurerm_storage_account.st-presettie.id
  principal_id         = azurerm_user_assigned_identity.ado-pipeline-identity.principal_id
  role_definition_name = "Storage Blob Data Contributor"
}

# App Service

resource "azurerm_service_plan" "asp-presettie" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  name     = "asp-presettie-${var.env}"
  os_type  = "Linux"
  sku_name = "FC1"

  tags = local.tags
}

resource "azurerm_function_app_flex_consumption" "func-presettie-bot" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  service_plan_id = azurerm_service_plan.asp-presettie.id

  name = "func-presettie-bot-${var.env}"

  runtime_name    = "dotnet-isolated"
  runtime_version = "9.0"

  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.st-presettie.primary_access_key
  storage_container_endpoint  = "${azurerm_storage_account.st-presettie.primary_blob_endpoint}${azurerm_storage_container.stc-bot-deployments-presettie.name}"
  storage_container_type      = "blobContainer"

  instance_memory_in_mb  = 512
  maximum_instance_count = 10

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.appi-presettie.connection_string
  }

  app_settings = merge(
    {
      GeneratorSchedule      = var.generator-schedule
      KeyVaultName           = azurerm_key_vault.kv-presettie.name
      Resources__DefaultLang = var.resources-default-lang

      Auth__CallbackUrl  = var.auth-callback-url
      Database__Name     = var.database-name
      Storage__QueueName = azurerm_storage_queue.stq-requests-presettie.name
    },
    {
      for idx, scope in var.auth-scopes : "Auth__Scopes__${idx}" => scope
    }
  )

  tags = local.tags
}

resource "azurerm_function_app_flex_consumption" "func-presettie-api" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  service_plan_id = azurerm_service_plan.asp-presettie.id

  name = "func-presettie-api-${var.env}"

  runtime_name    = "dotnet-isolated"
  runtime_version = "9.0"

  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.st-presettie.primary_access_key
  storage_container_endpoint  = "${azurerm_storage_account.st-presettie.primary_blob_endpoint}${azurerm_storage_container.stc-api-deployments-presettie.name}"
  storage_container_type      = "blobContainer"

  instance_memory_in_mb  = 512
  maximum_instance_count = 10

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.appi-presettie.connection_string
  }

  app_settings = merge(
    {
      KeyVaultName = azurerm_key_vault.kv-presettie.name,

      Auth__CallbackUrl      = var.auth-callback-url
      Resources__DefaultLang = var.resources-default-lang
      Database__Name         = var.database-name,
      Storage__QueueName     = azurerm_storage_queue.stq-requests-presettie.name
    },
    {
      for idx, scope in var.auth-scopes : "Auth__Scopes__${idx}" => scope
    }
  )

  tags = local.tags
}

resource "azurerm_storage_account_static_website" "st-sw-presettie" {
  storage_account_id = azurerm_storage_account.st-presettie.id

  error_404_document = "index.html"
  index_document     = "index.html"
}
