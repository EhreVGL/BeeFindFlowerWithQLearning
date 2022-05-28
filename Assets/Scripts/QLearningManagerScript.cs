using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


/*
 *  KONTROLLER:
 *      Mouse-1: Arýlarý koyar.
 *      Mouse-2: Çiçeði koyar.
 *      Mouse-3: Çalýyý koyar.
 *      Space: Tilemap ile oynamayý kapatýr.
 */




public class QLearningManagerScript : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Tile[] tiles;

    [SerializeField] private int redTileCount = 40;
    [SerializeField] private bool itsOK = false;
    private Vector3Int FlowerLoc;
    private Vector3Int BeeLoc;
    private Vector3Int CurrentBeeLoc;
    private Vector3Int NextBeeLoc;
    private int[,] R = new int[100,8];
    private float[,] Q = new float[100,8];

    [SerializeField] private int currentState;
    [SerializeField] private int nextState;
    [SerializeField] private int currentAction;
    [SerializeField] private int oldAction = 9;
    private bool passedState = false;
    public int loop = 20;
    public float waitTime;
    public List<Vector3Int> iterationPath;
    public List<Vector3Int> bestStatePath;

    private float epsilone = 0.8f;
    private bool trainingDone = false;


    private int choosenActionReplayCounter = 0;
    private bool reset = true;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(tilemap.size);
        tilemap.ClearAllTiles();
        Debug.Log(tilemap.size);


        // 11x11 tile
        for (int i = -6; i < 6; i++)
        {
            for (int j = -6; j < 6; j++)
            {
                if (i == -6 || i == 5 || j == -6 || j == 5)
                {
                    tilemap.SetTile(new Vector3Int(i, j, 0), tiles[4]);
                }
                else
                {
                    tilemap.SetTile(new Vector3Int(i, j, 0), tiles[0]);
                }
            }
        }
        while (redTileCount > 0)
        {
            int randomx = Random.Range(-5, 5);
            int randomy = Random.Range(-5, 5);
            tilemap.SetTile(new Vector3Int(randomx, randomy, 0), tiles[4]);
            redTileCount--;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Space'e basýldýðýnda tilemap ile oynama kapatýlýyor ve kod baþlýyor.
        if (Input.GetKey(KeyCode.Space) && itsOK == false)
        {

            itsOK = true;
            CreateMatris();
            while(passedState == false)
            {
                chooseAction();
            }
//            CalculateQ();
            StartCoroutine(move());


        }

        // tilemap ile oynama kýsmý
        if (!itsOK)
        {
            // Arý
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 loc = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int locInt = tilemap.WorldToCell(loc);
                if(!(locInt.x < -5 ||locInt.y > 4))
                {
                    for (int i = -5; i < 5; i++)
                    {
                        for (int j = -5; j < 5; j++)
                        {
                            if (tilemap.GetTile(new Vector3Int(i, j, 0)).name == tiles[1].name)
                            {
                                tilemap.SetTile(new Vector3Int(i, j, 0), tiles[0]);
                            }
                        }
                    }
                    tilemap.SetTile(locInt, tiles[1]);
                    currentState = ((locInt.y + 5) * 10) + (locInt.x + 5);
                    BeeLoc = locInt;
                    CurrentBeeLoc = locInt;
                    //Debug.Log(currentState);
                }

            }
            // Cicek
            else if (Input.GetMouseButtonDown(1))
            {
                Vector3 loc = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int locInt = tilemap.WorldToCell(loc);
                if (!(locInt.x < -5 || locInt.y > 4))
                {
                    for (int i = -5; i < 5; i++)
                    {
                        for (int j = -5; j < 5; j++)
                        {
                            if (tilemap.GetTile(new Vector3Int(i, j, 0)).name == tiles[3].name)
                            {
                                tilemap.SetTile(new Vector3Int(i, j, 0), tiles[0]);
                            }
                        }
                    }
                    tilemap.SetTile(locInt, tiles[3]);
                    FlowerLoc = locInt;
                }
            }
            else if (Input.GetMouseButtonDown(2))
            {
                Vector3 loc = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int locInt = tilemap.WorldToCell(loc);
                if (!(locInt.x < -5 || locInt.y > 4))
                {
                    if (!(tilemap.GetTile(locInt).name == tiles[1].name || (tilemap.GetTile(locInt).name == tiles[3].name)))
                    {
                        if (tilemap.GetTile(locInt).name == tiles[0].name)
                        {
                            tilemap.SetTile(locInt, tiles[4]);
                        }
                        else if (tilemap.GetTile(locInt).name == tiles[4].name)
                        {
                            tilemap.SetTile(locInt, tiles[0]);
                        }
                    }
                }
            }
            // Harita dýþý tile'larýn hepsini çalý yapar.
            for (int i = -6; i < 6; i++)
            {
                for (int j = -6; j < 6; j++)
                {
                    if (i == -6 || i == 5 || j == -6 || j == 5)
                    {
                        tilemap.SetTile(new Vector3Int(i, j, 0), tiles[4]);
                    }
                }
            }
        }
    }
    private void FixedUpdate()
    {

    }

    // Bu kýsýmda Q ve R matrislerinin default halleri oluþturuluyor.
    // Sað,Sol,Yukarý,Aþaðý,SaðÜst,SolAlt,SaðAlt,SolÜst => 0,1,2,3,4,5,6,7
    private void CreateMatris()
    {
        for(int i = -5; i < 5; i++)
        {
            for(int j = -5; j <5; j++)
            {
                for(int action_count = 0; action_count < 8; action_count++)
                {
                    Q[((j + 5) * 10) + (i + 5), action_count] = 0;
                    // R matrisinde R[state,0] durumu
                    if(action_count == 0)
                    {
                        if(tilemap.GetTile(new Vector3Int(i+1,j,0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if(tilemap.GetTile(new Vector3Int(i+1,j,0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                    // R matrisinde R[state,1] durumu
                    else if (action_count == 1)
                    {
                        if (tilemap.GetTile(new Vector3Int(i - 1, j, 0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if (tilemap.GetTile(new Vector3Int(i - 1, j, 0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                    // R matrisinde R[state,2] durumu
                    else if (action_count == 2)
                    {
                        if (tilemap.GetTile(new Vector3Int(i, j + 1, 0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if (tilemap.GetTile(new Vector3Int(i, j + 1, 0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                    // R matrisinde R[state,3] durumu
                    else if (action_count == 3)
                    {
                        if (tilemap.GetTile(new Vector3Int(i, j - 1, 0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if (tilemap.GetTile(new Vector3Int(i, j - 1, 0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                    // R matrisinde R[state,4] durumu
                    else if (action_count == 4)
                    {
                        if (tilemap.GetTile(new Vector3Int(i + 1, j + 1, 0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if (tilemap.GetTile(new Vector3Int(i + 1, j + 1, 0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                    // R matrisinde R[state,5] durumu
                    else if (action_count == 5)
                    {
                        if (tilemap.GetTile(new Vector3Int(i - 1, j - 1, 0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if (tilemap.GetTile(new Vector3Int(i - 1, j - 1, 0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                    // R matrisinde R[state,6] durumu
                    else if (action_count == 6)
                    {
                        if (tilemap.GetTile(new Vector3Int(i + 1, j - 1, 0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if (tilemap.GetTile(new Vector3Int(i + 1, j - 1, 0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                    // R matrisinde R[state,7] durumu
                    else if (action_count == 7)
                    {
                        if (tilemap.GetTile(new Vector3Int(i - 1, j + 1, 0)).name == tiles[4].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = -1;
                        }
                        else if (tilemap.GetTile(new Vector3Int(i - 1, j + 1, 0)).name == tiles[3].name)
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 100;
                        }
                        else
                        {
                            R[((j + 5) * 10) + (i + 5), action_count] = 0;
                        }
                    }
                }
            }
        }
    }

    /* Burada Action seçiliyor.
     * 
     * Yapýlan iþlem:
     *      Bulunan state'teki boþ olan actionlarýn hepsi sayýlýyor ve bunlar arasýnda bir random deðer atanýp
     *    bu atýlan random deðer currentAction deðiþkenine eþleniyor.
     * 
     */
    private void chooseAction()
    {
        passedState = true;
        int notRedTileCount = 0;
        for (int action_count = 0; action_count < 8; action_count++)
        {
            if (R[currentState, action_count] != -1)
            {
                notRedTileCount++;
            }
        }
        // Eðer ilk hamleyse ve agent'in bulunduðu state'in etrafý 8 tile çalý ise HaritaHatali olur ve kendi yol açar.
        if (notRedTileCount == 0)
        {
            //currentAction = 0;
            HataliHarita();
        }
        else
        {
            int random = Random.Range(0, notRedTileCount);
            for (int j = 0; j < 8; j++)
            {
                if (R[currentState, j] != -1)
                {
                    if (random == 0)
                    {
                        if (iterationPath.Count != 0)
                        {
                           // Debug.Log("CurrentAction iterationPath sorgulandý.");
                            GecildiMi(j);
                        }
                        if (passedState == true)
                        {
                           // Debug.Log("passedState true sorgulandý.");

                            if (Mathf.Abs(oldAction - j) == 1)
                            {
                               // Debug.Log("CurrentAction eski yolla ayný çýktý");
                                passedState = false;
                            }
                            else
                            {
                                //Debug.Log("CurrentAction Degisti.");
                                currentAction = j;
                                break;
                            }
                        }
                    }
                    random--;
                }
            }
        }
    }

    // Þu anki iteration'da geçilen yol üzerinde mi diye sorgulamayý saðlayan fonksiyon. Bu fonksiyon sayesinde sürekli ayný yolu izleyerek ilerleme
    // ihtimali ortadan kaldýrýlýyor ve geçtiði bir tile'dan geçmek yerine yeniden chooseAction() fonksiyonuna gidilerek yeni bir yol seçiliyor.
    private void GecildiMi(int j)
    {
        Vector3Int GonnaState = CurrentBeeLoc;
        if (j == 0)
        {
            GonnaState.x += 1;
            foreach(Vector3Int path in iterationPath)
            {
                if(path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
        else if (j == 1)
        {
            GonnaState.x -= 1; 
            foreach (Vector3Int path in iterationPath)
            {
                if (path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
        else if (j == 2)
        {
            GonnaState.y += 1;
            foreach (Vector3Int path in iterationPath)
            {
                if (path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
        else if (j == 3)
        {
            GonnaState.y -= 1;
            foreach (Vector3Int path in iterationPath)
            {
                if (path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
        else if (j == 4)
        {
            GonnaState.x += 1;
            GonnaState.y += 1;
            foreach (Vector3Int path in iterationPath)
            {
                if (path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
        else if (j == 5)
        {
            GonnaState.x -= 1;
            GonnaState.y -= 1;
            foreach (Vector3Int path in iterationPath)
            {
                if (path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
        else if (j == 6)
        {
            GonnaState.x += 1;
            GonnaState.y -= 1;
            foreach (Vector3Int path in iterationPath)
            {
                if (path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
        else if (j == 7)
        {
            GonnaState.x -= 1;
            GonnaState.y += 1;
            foreach (Vector3Int path in iterationPath)
            {
                if (path == GonnaState)
                {
                    passedState = false;
                    break;
                }
            }
            if (GonnaState.x < -6 || GonnaState.x > 5 || GonnaState.y < -6 || GonnaState.y > 5)
            {
                passedState = false;
            }
        }
    }

    // Gidilecek State'in bütün Action'larýndaki en büyük Q deðerinin bulunduðu fonksiyon.
    private float nextQmax()
    {
        float maxNextQ = 0;
        int nextStateAction = 0;
        for(int i = 0; i < 8; i++)
        {
            if(Q[nextState, nextStateAction] < Q[nextState, i])
            {
                maxNextQ = Q[nextState, i];
                nextStateAction = i;
            }
        }
        return maxNextQ;
    }
    // Arýyý hareket ettiren kýsým.
    private void BeeGo()
    {
        NextBeeLoc = CurrentBeeLoc;
        if(currentAction == 0)
        {
            NextBeeLoc.x += 1;
        }
        else if(currentAction == 1)
        {
            NextBeeLoc.x -= 1;
        }
        else if (currentAction == 2)
        {
            NextBeeLoc.y += 1;
        }
        else if (currentAction == 3)
        {
            NextBeeLoc.y -= 1;
        }
        else if (currentAction == 4)
        {
            NextBeeLoc.x += 1;
            NextBeeLoc.y += 1;
        }
        else if (currentAction == 5)
        {
            NextBeeLoc.x -= 1;
            NextBeeLoc.y -= 1;
        }
        else if (currentAction == 6)
        {
            NextBeeLoc.x += 1;
            NextBeeLoc.y -= 1;
        }
        else if (currentAction == 7)
        {
            NextBeeLoc.x -= 1;
            NextBeeLoc.y += 1;
        }
        Debug.Log(NextBeeLoc.x + "X|Y " + NextBeeLoc.y);
        if(tilemap.GetTile(NextBeeLoc).name == tiles[3].name)
        {
            FoundFlower();
        }
        else
        {
            if(tilemap.GetTile(NextBeeLoc).name != tiles[4].name)
            {
                tilemap.SetTile(NextBeeLoc, tiles[1]);
                if (tilemap.GetTile(CurrentBeeLoc).name != tiles[4].name)
                {
                    tilemap.SetTile(CurrentBeeLoc, tiles[0]);
                }
            }
            nextState = ((NextBeeLoc.y + 5) * 10) + (NextBeeLoc.x + 5);
            CurrentBeeLoc = NextBeeLoc;
            oldAction = currentAction;
            iterationPath.Add(CurrentBeeLoc);
        }
        passedState = false;
    }

    private void HataliHarita()
    {
        Debug.Log("Path'e reset atildi.");
        passedState = true;
    }

    private void ResetPath()
    {
        tilemap.SetTile(CurrentBeeLoc, tiles[0]);
        tilemap.SetTile(BeeLoc, tiles[1]);
        CurrentBeeLoc = BeeLoc;
        NextBeeLoc = CurrentBeeLoc;
        currentState = ((BeeLoc.y + 5) * 10) + (BeeLoc.x + 5);
        currentAction = 0;
        oldAction = 9;
        //reset = true;
        iterationPath.Clear();

    }
    private void FoundFlower()
    {
        tilemap.SetTile(CurrentBeeLoc, tiles[0]);
        tilemap.SetTile(BeeLoc, tiles[1]);
        CurrentBeeLoc = BeeLoc;
        NextBeeLoc = CurrentBeeLoc;
        currentState = ((BeeLoc.y + 5) * 10) + (BeeLoc.x + 5);
        currentAction = 0;
        oldAction = 9;
        reset = true;
        BestPath(true);
        iterationPath.Clear();
    }

    private void BestPath(bool done)
    {
        if (done)
        {

            loop -= 1;
            if (bestStatePath.Count == 0)
            {
                for (int i = 0; i < iterationPath.Count; i++)
                {
                    bestStatePath.Add(iterationPath[i]);
                }

            }
            else
            {
                if (bestStatePath.Count > iterationPath.Count)
                {
                    bestStatePath.Clear();
                    for (int i = 0; i < iterationPath.Count; i++)
                    {
                        bestStatePath.Add(iterationPath[i]);
                    }

                }
            }
            if (loop == 0)
            {
                for (int i = 0; i < bestStatePath.Count; i++)
                {
                    tilemap.SetTile(bestStatePath[i], tiles[2]);
                    trainingDone = true;
                }
            }
        }
    }
    private void CalculateQ()
    {
        while(trainingDone == false)
        {
            while(reset == false)
            {
                Q[currentState, currentAction] = R[currentState, currentAction] + (epsilone * nextQmax());
                BeeGo();
                currentState = nextState;
                chooseAction();

                //while (passedState == false)
                //{
                //    if(choosenActionReplayCounter == 5)
                //    {
                //        ResetPath();
                //    }
                //    else
                //    {
                //        chooseAction();
                //        choosenActionReplayCounter++;
                //    }
                //}
                //choosenActionReplayCounter = 0;
            }
            reset = false;
        }

    }

    IEnumerator move()
    {
        while (trainingDone == false)
        {
            reset = false;

            while (reset == false)
            {
                yield return new WaitForSeconds(waitTime);
                Q[currentState, currentAction] = R[currentState, currentAction] + (epsilone * nextQmax());
                BeeGo();
                currentState = nextState;

                while (passedState == false)
                {
                    if (choosenActionReplayCounter == 5)
                    {
                        ResetPath();
                        Debug.Log("ResetPath ######");
                        choosenActionReplayCounter = 0;
                    }
                    else
                    {
                        chooseAction();
                        Debug.Log("CurrentAction: " + currentAction);
                        choosenActionReplayCounter++;
                    }
                }
                choosenActionReplayCounter = 0;
            }
        }
        EndGame();
    }

    private void EndGame()
    {
        for(int i = 0; i < 100; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                Debug.Log("Q[" + i + "," + j + "]: " + Q[i, j]);
            }
        }
    }
}
