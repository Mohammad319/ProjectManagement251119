using Application.Feature.Project.Folder.Commands;
using Application.Feature.Project.Project.Commands;
using AutoMapper;
using Domain.Entities.Folder;
using ProjectManagement.Shared.DTO.Folder;

namespace Application.Profiles
{
    public class FolderProfile : Profile
    {
        public FolderProfile()
        {
            CreateMap<FolderEntity, PostFolderDTO>().ReverseMap();
        }
    }
}
