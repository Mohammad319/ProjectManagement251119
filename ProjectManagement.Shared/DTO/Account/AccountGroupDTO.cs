using ProjectManagement.Shared.Base.Account;
using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.Account
{
    public class PostAccountGroupDTO : AccountGroupBase
    {
    }
    public class PostAccountGroupWithAccountsDTO : AccountGroupBase
    {
        public List<PostAccountDTO> Accounts { get; set; } = [];
    }
    public class ListAccountGroupIncludeAccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ListAccountDTO> Accounts { get; set; } = [];
    }

    /// <summary>Outcome of a Kontoplan import (upsert). Used to give the admin a clear summary after Save.</summary>
    public class AccountImportResultDTO
    {
        public bool Success { get; set; }
        public int GroupsCreated { get; set; }
        public int AccountsCreated { get; set; }
        public int AccountsUpdated { get; set; }
        public int AccountsSkipped { get; set; }
        public int ImportBatchId { get; set; }
    }

    /// <summary>Extra information stored with a saved Kontoplan import so it shows up under "Senaste importer".</summary>
    public class AccountImportBatchInfoDTO
    {
        public string FileName { get; set; } = string.Empty;

        /// <summary>Internal import-type key, e.g. "Table" or "PairColumns".</summary>
        public string ImportType { get; set; } = string.Empty;

        /// <summary>Snapshot of the rows the admin saved (for "Visa importerade rader").</summary>
        public List<AccountImportBatchRowDTO> Rows { get; set; } = [];
    }

    /// <summary>One row of a saved import batch, as shown in "Visa importerade rader".</summary>
    public class AccountImportBatchRowDTO
    {
        public string Group { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>What the import did with the row: "Ny", "Uppdaterad" or "Hoppades över".</summary>
        public string Action { get; set; } = string.Empty;
    }

    /// <summary>A saved Kontoplan import shown in the "Senaste importer" list.</summary>
    public class AccountImportBatchDTO
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ImportType { get; set; } = string.Empty;
        public System.DateTime CreatedAt { get; set; }
        public string ImportedBy { get; set; } = string.Empty;
        public int GroupsCreated { get; set; }
        public int AccountsCreated { get; set; }
        public int AccountsUpdated { get; set; }
        public int AccountsSkipped { get; set; }
        public bool IsUndone { get; set; }

        /// <summary>Undo is only safe when the batch did not update existing accounts.</summary>
        public bool CanUndo => !IsUndone && AccountsUpdated == 0 && (AccountsCreated > 0 || GroupsCreated > 0);
    }
}
