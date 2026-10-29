namespace Jobsy.Core.Scholen.PupilPassport;

public interface IParentPassportAnswersPdfService
{
    byte[] Render(ParentPassportAnswersPdfModel model);
}
