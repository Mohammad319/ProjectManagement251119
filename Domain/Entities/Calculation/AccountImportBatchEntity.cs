using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Calculation
{
    /// <summary>
    /// One saved Kontoplan import (file → accounts/groups). Keeps enough information to show
    /// "Senaste importer" and to undo an import that only created new rows: the ids of the
    /// groups/accounts the batch created plus a JSON snapshot of the imported rows.
    /// Imports that updated existing accounts cannot be undone (no old-value history is kept).
    /// </summary>
    public sealed class AccountImportBatchEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.LongName)]
        public string FileName { get; private set; } = string.Empty;

        /// <summary>Internal key for the import type, e.g. "Table" or "PairColumns".</summary>
        [Required, MaxLength(FieldLengths.ShortName)]
        public string ImportType { get; private set; } = string.Empty;

        public int GroupsCreated { get; private set; }
        public int AccountsCreated { get; private set; }
        public int AccountsUpdated { get; private set; }
        public int AccountsSkipped { get; private set; }

        /// <summary>JSON int array with the ids of the account groups this batch created.</summary>
        public string CreatedGroupIdsJson { get; private set; } = "[]";

        /// <summary>JSON int array with the ids of the accounts this batch created.</summary>
        public string CreatedAccountIdsJson { get; private set; } = "[]";

        /// <summary>JSON snapshot of the imported rows (group, code, name, action) for "Visa importerade rader".</summary>
        public string RowsJson { get; private set; } = "[]";

        public bool IsUndone { get; private set; }

        private AccountImportBatchEntity() { }

        public AccountImportBatchEntity(
            string fileName,
            string importType,
            int groupsCreated,
            int accountsCreated,
            int accountsUpdated,
            int accountsSkipped,
            string createdGroupIdsJson,
            string createdAccountIdsJson,
            string rowsJson)
        {
            if (string.IsNullOrWhiteSpace(importType))
                throw new ValidationException($"{nameof(ImportType)} is required.");

            FileName = (fileName ?? string.Empty).Trim();
            if (FileName.Length > FieldLengths.LongName)
                FileName = FileName[..FieldLengths.LongName];

            ImportType = importType.Trim();
            GroupsCreated = groupsCreated;
            AccountsCreated = accountsCreated;
            AccountsUpdated = accountsUpdated;
            AccountsSkipped = accountsSkipped;
            CreatedGroupIdsJson = string.IsNullOrWhiteSpace(createdGroupIdsJson) ? "[]" : createdGroupIdsJson;
            CreatedAccountIdsJson = string.IsNullOrWhiteSpace(createdAccountIdsJson) ? "[]" : createdAccountIdsJson;
            RowsJson = string.IsNullOrWhiteSpace(rowsJson) ? "[]" : rowsJson;
        }

        public void MarkUndone() => IsUndone = true;
    }
}
