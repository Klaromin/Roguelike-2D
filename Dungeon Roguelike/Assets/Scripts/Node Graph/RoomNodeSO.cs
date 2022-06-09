using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoomNodeSO : ScriptableObject
{
    public string id;
    public List<string> parentRoomNodeIDList = new List<string>();
    public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

    #region Editor Code
#if UNITY_EDITOR
    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;
        //Load room node type list
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    public void Draw(GUIStyle nodeStyle)
    {
        //Draw Node Box Using Begin Area
        GUILayout.BeginArea(rect, nodeStyle);
        //Start Region to Detect Popup Selection Changes
        EditorGUI.BeginChangeCheck();

        if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEnterance)
        {
            //Display a label that can't be changed
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName); // a�a��daki gibi popup ile se�me se�ene�i bulundurmak yerine, parent� yok ya da enterance ise Label ile ba�l�k veriyoruz
        }
        else
        {
            //Display a popup using the RoomNodeType name values that can be selected from(default to the curerently set roomNodeType)
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];

            //E�er oda tipi de�i�tiyse child ba�lant�lar�n� potansiyel olarak kes
            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isCorridor &&
                roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
            {
                if (childRoomNodeIDList.Count > 0)
                {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        //Get child room node
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);
                        //if child room node is selected
                        if (childRoomNode != null)
                        {
                            //remove childID from parent room node
                            RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);
                            //remove parenID from parent room node
                            childRoomNode.RemoveParenRoomNodeIDFromRoomNode(id);



                        }
                    }
                }
            }

        }
        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(this);

        GUILayout.EndArea();
    }





    

    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count]; //room node typelist listesinin �ye say�s� kadar bir bo� array olu�turduk.

        for (int i = 0; i < roomNodeTypeList.list.Count; i++)// yine ayn� liste �ye say�s� kadar for d�ng�s� ba�latt�k.
        {

            if (roomNodeTypeList.list[i].displayInNodeGraphEditor) // e�er room type bizim display etmek istedi�imiz bir type ise
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName; // onu room arrayine ekledik. (RoomNodeTypeSO'da belirtmi�tik hangilerini display etmek istedi�imizi)
            }


        }
        return roomArray;
    }

    public void ProcessEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;
            default:
                break;
        }

    }
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        if (currentEvent.button == 0) // 0 sol click temsil ediyor.
        {
            ProcessLeftClickDownEvent();
        }
        else if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);

        }

    }

    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;
        //isSelected = !isSelected; //Bu da a�a��daki bloklarla ayn� i�i g�r�yor.
        //Toggle node selection
        if (isSelected == true)
        {
            isSelected = false;
        }
        else
        {
            isSelected = true;
        }
    }

    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }

    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }

    private void ProcessLeftClickUpEvent()
    {
        if (isLeftClickDragging)
        {
            isLeftClickDragging = false;
        }
    }

    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }

    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;

        DragNode(currentEvent.delta);
        GUI.changed = true;
    }

    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    public bool AddChildRoomNodeIDToRoomNode(string childID)
    {
        if (IsChildRoomValid(childID)) //Check child node can be added validly to parent
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }
        return false;
        
       
    }


    public bool IsChildRoomValid(string childID)
    {
        bool isConnectedBossNodeAlready = false;
        // check if there is there already a connected boss room in the node graph
        foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0) //tip boss roomsa ve 1 adet ba�lant�s� yani parent� varsa;
            {
                isConnectedBossNodeAlready = true;
            }

        }
        //e�er child node boss room tipindeyse ve boss rooma ba�land�ysa false d�n yani 2. bir boss rooma ba�lanama
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
            return false;
        //e�er child node ba�alnt�ya izin vermedei�imiz tiplerdense
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
            return false;
        //e�er nodeumuz belirtilen child'a sahipse tekrar ba�lant� yapma. 2 kere ayn� nodeu ba�lamam�z� engelliyor.
        if (childRoomNodeIDList.Contains(childID))
            return false;
        //e�er ba�lan� kendisi ise ba�lama
        if (id == childID)
            return false;
        //e�er bu childID �okten parentID listte ise ba�lama. Yani bir node di�erinin parent�yken child nodeu o parenta parent yapama.
        if (parentRoomNodeIDList.Contains(childID))
            return false;
        // child node �oktan bir parenta sahipse false d�n. Bu bizim design kural�m�zda vard�.
        if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
            return false;
        // koridorlar� birbirine ba�lamay� engelle
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
            return false;
        //bizim odalar�m�z sadece koridorlar arac�l��� ile birbirine ba�lanabilir design kural�.
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
            return false;
        //e�er izin verilenden fazla child koridor varsa ba�lama.Settingste buna 3 demi�tik dizayn ku�ral� bu da.

        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
            return false;
        //e�er child room enterance ise false d�n enterance her zaman en b�y�k parent olmal�
        if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEnterance)
            return false;
        // e�er bir roomu koridora ekliyorsak. bu koridorun �oktan bir roomu olmamas� laz�m. bir koridor tek bir odaya ��kar mant���
        if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
            return false;

        return true;


    }

    public bool AddParentRoomNodeIDToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }

    public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
    {
        //e�er node bir child ID bar�nd�r�yorsa sil onu
        if (childRoomNodeIDList.Contains(childID))
        {
            childRoomNodeIDList.Remove(childID);
            return true;
        }
        return false;
    }

    public bool RemoveParenRoomNodeIDFromRoomNode(string parentID)
    {
        //e�er node bir child ID bar�nd�r�yorsa sil onu
        if (parentRoomNodeIDList.Contains(parentID))
        {
            parentRoomNodeIDList.Remove(parentID);
            return true;
        }
        return false;
    }
#endif

    #endregion Editor Code
}
