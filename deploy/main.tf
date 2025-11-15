terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=4.38.1"
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

resource "azurerm_application_insights" "appi-presettie" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

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

resource "azurerm_storage_queue" "stq-requests-presettie" {
  storage_account_name = azurerm_storage_account.st-presettie.name

  name = var.storage-queue-name
}

resource "azurerm_service_plan" "asp-presettie" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  name     = "asp-presettie-${var.env}"
  os_type  = "Linux"
  sku_name = "Y1"

  tags = local.tags
}

resource "azurerm_linux_function_app" "func-presettie-bot" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  storage_account_name       = azurerm_storage_account.st-presettie.name
  storage_account_access_key = azurerm_storage_account.st-presettie.primary_access_key
  service_plan_id            = azurerm_service_plan.asp-presettie.id

  name = "func-presettie-bot-${var.env}"

  functions_extension_version = "~4"

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_key = azurerm_application_insights.appi-presettie.instrumentation_key
    app_scale_limit          = 10

    application_stack {
      dotnet_version              = "9.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = merge(
    {
      Database__Name            = var.database-name
      GeneratorSchedule         = var.generator-schedule
      Storage__ConnectionString = azurerm_storage_account.st-presettie.primary_connection_string
      Storage__QueueName        = azurerm_storage_queue.stq-requests-presettie.name
      Reccobeats__Url           = var.reccobeats-url
      Resources__DefaultLang    = var.resources-default-lang
      KeyVault__Uri             = azurerm_key_vault.kv-presettie.vault_uri
    },
    {
      for idx, scope in var.auth-scopes : "Auth__Scopes__${idx}" => scope
  })

  tags = local.tags
}

resource "azurerm_linux_function_app" "func-presettie-api" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  storage_account_name       = azurerm_storage_account.st-presettie.name
  storage_account_access_key = azurerm_storage_account.st-presettie.primary_access_key
  service_plan_id            = azurerm_service_plan.asp-presettie.id

  name = "func-presettie-api-${var.env}"

  functions_extension_version = "~4"

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_key = azurerm_application_insights.appi-presettie.instrumentation_key
    app_scale_limit          = 10

    application_stack {
      dotnet_version              = "9.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = merge(
    {
      Database__Name         = var.database-name
      Resources__DefaultLang = var.resources-default-lang
      Auth__Audience         = var.jwt-audience
      Storage__QueueName     = azurerm_storage_queue.stq-requests-presettie.name
      KeyVaultName           = azurerm_key_vault.kv-presettie.name
  })

  tags = local.tags
}

resource "azurerm_storage_account_static_website" "st-sw-presettie" {
  storage_account_id = azurerm_storage_account.st-presettie.id

  error_404_document = "index.html"
  index_document     = "index.html"
}
