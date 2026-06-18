using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Base
{
    /// <summary>
    /// Ordered, visibility-aware lookup WITHOUT a colour.
    /// It mirrors <see cref="ColoredListEntity"/>'s public surface so it can flow through the shared
    /// <see cref="IListOrderDTO"/> lookup infrastructure, but it never stores a colour:
    /// <see cref="Color"/> is a non-mapped no-op kept only to satisfy the shared contract.
    /// </summary>
    public abstract class OrderedListEntity : AuditableEntity<int>, IListOrderDTO
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; protected set; } = string.Empty;

        public int SortOrder { get; protected set; }
        public bool IsVisible { get; protected set; } = true;

        /// <summary>Not stored. Present only to satisfy the shared IListOrderDTO contract; always empty.</summary>
        [NotMapped]
        public string Color => string.Empty;

        protected OrderedListEntity() { }

        // The colour argument is intentionally ignored so existing call sites compile unchanged.
        protected OrderedListEntity(string name, string color, int sortOrder, bool isVisible = true)
            => Update(name, color, sortOrder, isVisible);

        public void Update(string name, string color, int sortOrder, bool isVisible)
        {
            Name = NormalizeName(name);
            SortOrder = NormalizeSortOrder(sortOrder);
            IsVisible = isVisible;
        }

        private static string NormalizeName(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Name is required.");
            if (trimmed.Length > FieldLengths.Name)
                throw new ValidationException($"Name exceeds max length {FieldLengths.Name}.");
            return trimmed;
        }

        private static int NormalizeSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new ValidationException("SortOrder cannot be negative.");
            return sortOrder;
        }
    }
}
