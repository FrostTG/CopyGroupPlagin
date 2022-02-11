using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlagin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                GroupPickFilter groupPickFilter = new GroupPickFilter();
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                Element element = doc.GetElement(reference);
                Group group = element as Group;

                XYZ groupCenter = GetElementCenter(group);
                Room room = GetRoomByPoint(doc, groupCenter); //Определяем комнату, где находится выбранная группа объектов
                XYZ roomCenter = GetElementCenter(room); //Находим центр этой комнаты
                XYZ offset = groupCenter - roomCenter; //Определяем смещение центр группы относительно центра комнаты

                XYZ point = uiDoc.Selection.PickPoint("Выберите точку"); //выбираем точку внутри комнаты         
                Room selectRoom = GetRoomByPoint(doc, point);               
                XYZ selectRoomCenter = GetElementCenter(selectRoom);
                XYZ offset1 = selectRoomCenter+offset;

                using (Transaction ts = new Transaction(doc))
                {
                    ts.Start("Копирование группы объектов");
                    Group groupCopy=doc.Create.PlaceGroup(offset1, group.GroupType);
                    groupCopy.get_Parameter(BuiltInParameter.GROUP_OFFSET_FROM_LEVEL).Set(0); //меняем смещение иначе компьютер будет под столом
                    ts.Commit();
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }
        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }
    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
